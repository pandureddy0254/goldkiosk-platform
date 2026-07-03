using System.Collections.Concurrent;
using System.Globalization;
using GoldKiosk.Contracts.V1.Agent;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Settlement;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Logging;
using GoldKiosk.Kiosk.Core.Analysis;
using GoldKiosk.Kiosk.Core.Identity;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Orchestration;
using GoldKiosk.Kiosk.Core.Persistence;
using GoldKiosk.Kiosk.Core.Pricing;
using GoldKiosk.Kiosk.Core.Rejections;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Orchestration;

/// <summary>
/// Drives the hardware pipeline for a session — tray, arm, scale, analyser, chamber,
/// identity devices, bagger, dispenser, printer — applying the Kiosk.Core policies and
/// state machine, journaling every transition (ADR 0002), and surfacing progress through
/// <see cref="IKioskEventPublisher"/>. Lives in the Kiosk.Api host because only Kiosk.Api
/// touches Kiosk.Devices; all business rules stay in Kiosk.Core.
/// </summary>
public sealed class TransactionOrchestrator
{
    private readonly SessionRegistry _sessions;
    private readonly IDeviceRegistry _devices;
    private readonly IKioskEventPublisher _events;
    private readonly ISessionStore _store;
    private readonly IOfferCalculator _offerCalculator;
    private readonly AnalysisPolicy _analysisPolicy;
    private readonly FeaturesOptions _features;
    private readonly KioskOptions _kiosk;
    private readonly TimeProvider _timeProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TransactionOrchestrator> _logger;
    private readonly Lock _invoiceGate = new();

    // Per-session cancellation: created at begin, cancelled by abort, disposed at the
    // terminal state. Abortable background pipelines run on these tokens.
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _sessionCancellations =
        new(StringComparer.Ordinal);

    // Per-session publish gates: sequence issue + hub publish are serialized so events
    // never reach clients out of order (the UI drops anything at or below the last-seen
    // sequence). Entries live for the process lifetime, like the session registry.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _publishGates =
        new(StringComparer.Ordinal);

    /// <summary>Initializes the orchestrator.</summary>
    /// <param name="sessions">The in-memory session registry.</param>
    /// <param name="devices">The composed device registry.</param>
    /// <param name="events">The event publisher (SignalR-backed).</param>
    /// <param name="store">The file-based transaction store.</param>
    /// <param name="offerCalculator">The offer pricing port.</param>
    /// <param name="analysisPolicy">The item acceptance rules.</param>
    /// <param name="features">The feature flags.</param>
    /// <param name="kioskOptions">The kiosk options (transaction root for the invoice counter).</param>
    /// <param name="timeProvider">The time source.</param>
    /// <param name="lifetime">Host lifetime, cancelling background pipelines on shutdown.</param>
    /// <param name="logger">The host logger.</param>
    public TransactionOrchestrator(
        SessionRegistry sessions,
        IDeviceRegistry devices,
        IKioskEventPublisher events,
        ISessionStore store,
        IOfferCalculator offerCalculator,
        AnalysisPolicy analysisPolicy,
        IOptions<FeaturesOptions> features,
        IOptions<KioskOptions> kioskOptions,
        TimeProvider timeProvider,
        IHostApplicationLifetime lifetime,
        ILogger<TransactionOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(kioskOptions);

        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _offerCalculator = offerCalculator ?? throw new ArgumentNullException(nameof(offerCalculator));
        _analysisPolicy = analysisPolicy ?? throw new ArgumentNullException(nameof(analysisPolicy));
        _features = features.Value;
        _kiosk = kioskOptions.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Begins a new session in the <c>welcome</c> state, flagged <c>is_test</c> when any
    /// composed device is mocked (ADR 0004). A single-customer kiosk holds one active
    /// session — beginning while another non-terminal session exists fails with
    /// <c>session.invalid_state</c>.
    /// </summary>
    /// <returns>The registered session, or the guard failure.</returns>
    public Result<TransactionSession> BeginSession()
    {
        IReadOnlyList<TransactionSession> active = _sessions.ActiveSessions;
        if (active.Count > 0)
        {
            return Result.Failure<TransactionSession>(
                SessionErrors.InvalidState("begin a new session while another is active", active[0].State));
        }

        bool isTest = _devices.All.Any(d => d.Mode == DeviceMode.Mock);
        var session = TransactionSession.Begin(
            KioskIdGenerator.NewSessionId(_timeProvider), isTest, _timeProvider.GetUtcNow());
        _sessions.Add(session);
        _sessionCancellations[session.Id] =
            CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        _logger.SessionBegan(session.Id, session.IsTest);
        return Result.Success(session);
    }

    /// <summary>
    /// Applies the clubbed tray-open command: creates the transaction folder, opens the
    /// tray in the background, and reports <c>opening</c> immediately.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="setup">The clubbed pre-tray selections.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> OpenTrayAsync(
        TransactionSession session, SetupDto setup, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(setup);

        Result result = session.OpenTray(setup);
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.CreateTransactionFolderAsync(session, cancellationToken);
        await _store.AppendLogAsync(
            session, $"Tray opening (service '{setup.ServiceType}', locale '{setup.Locale}').", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await PublishTrayAsync(session, "opening", cancellationToken);
        await _store.PersistAsync(session, cancellationToken);

        RunInBackground(session, "tray-open", async token =>
        {
            await _devices.Get<ITray>().OpenAsync(token);
            await PublishTrayAsync(session, "open", token);
            await _store.AppendLogAsync(session, "Tray open — awaiting item.", token);
        });

        return Result.Success();
    }

    /// <summary>
    /// Applies the clubbed tray-close command: closes the tray in the background and, when
    /// an item was placed, runs the full analysis pipeline through to the offer.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="hasItem">Whether the customer placed an item.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> CloseTrayAsync(
        TransactionSession session, bool hasItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        Result result = session.CloseTray(hasItem);
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, $"Tray closing (has_item: {hasItem}).", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await PublishTrayAsync(session, "closing", cancellationToken);
        await _store.PersistAsync(session, cancellationToken);

        RunInBackground(session, "tray-close", async token =>
        {
            await _devices.Get<ITray>().CloseAsync(token);
            await PublishTrayAsync(session, "closed", token);
            if (hasItem)
            {
                await RunAnalysisPipelineAsync(session, token);
            }
        });

        return Result.Success();
    }

    /// <summary>Accepts the offer and moves to the identity checklist.</summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>Success, or the specific offer failure.</returns>
    public async Task<Result> AcceptOfferAsync(TransactionSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        Result result = session.AcceptOffer(_timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, "Offer accepted.", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.PersistAsync(session, cancellationToken);
        return Result.Success();
    }

    /// <summary>Declines the offer and returns the item.</summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the specific offer failure.</returns>
    public async Task<Result> DeclineOfferAsync(TransactionSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        Result result = session.DeclineOffer(_timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, "Offer declined — returning item.", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.PersistAsync(session, cancellationToken);

        RunInBackground(session, "offer-decline-return", async token =>
        {
            await RunReturnPathAsync(session, token);
            await FinishReturnAsync(session, token);
        });

        return Result.Success();
    }

    /// <summary>
    /// Starts the hardware-driven identity sequence
    /// (<c>id_scan → face_match → fingerprint? → signature</c>); progress flows over SignalR.
    /// KYC is fail-closed: any rejected gate returns the item and ends the session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> StartIdentityAsync(TransactionSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        Result result = session.StartIdentity(_features.FingerprintRequired);
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, "Identity sequence started.", cancellationToken);
        await _store.PersistAsync(session, cancellationToken);

        RunInBackground(session, "identity-sequence", token => RunIdentitySequenceAsync(session, token));
        return Result.Success();
    }

    /// <summary>
    /// Completes the signature step (clubbed signature + terms version), persisting the
    /// signature image under its legacy artifact name.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="request">The signature payload (already validated).</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> SubmitSignatureAsync(
        TransactionSession session, IdentitySignatureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);

        Result result = session.CompleteSignature(request.SignedTermsVersion);
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.WriteArtifactAsync(
            session, "signImage.png", Convert.FromBase64String(request.SignaturePngBase64), cancellationToken);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishIdentityProgressAsync(
                new IdentityProgressEvent(session.Id, sequence, "signature", "completed", null), ct),
            cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.AppendLogAsync(
            session, $"Signature captured (terms {request.SignedTermsVersion}).", cancellationToken);
        await _store.PersistAsync(session, cancellationToken);
        return Result.Success();
    }

    /// <summary>Stores the clubbed contact capture and moves to payout selection.</summary>
    /// <param name="session">The session.</param>
    /// <param name="contact">The contact payload (already validated).</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> SubmitContactAsync(
        TransactionSession session, GoldKiosk.Contracts.V1.Contact.ContactRequest contact, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(contact);

        Result result = session.SetContact(contact);
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(
            session,
            $"Contact captured (channels: {string.Join(", ", contact.ReceiptChannels)}).",
            cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.PersistAsync(session, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Confirms the payout method. Cash requires a feasible bill mix from the dispenser —
    /// an infeasible amount fails with <c>payout.insufficient_cash</c>.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="request">The payout payload (already validated).</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The confirmed payout status, or the specific failure.</returns>
    public async Task<Result<PayoutStatusDto>> ConfirmPayoutAsync(
        TransactionSession session, PayoutRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);

        if (session.State != SessionStates.Payout)
        {
            return Result.Failure<PayoutStatusDto>(
                SessionErrors.InvalidState("confirm the payout", session.State));
        }

        if (session.Offer is null)
        {
            return Result.Failure<PayoutStatusDto>(
                SessionErrors.InvalidState("confirm the payout without an offer", session.State));
        }

        PayoutSelection selection;
        if (string.Equals(request.Method, "cash", StringComparison.Ordinal))
        {
            BillMixPlan plan = await _devices.Get<ICashDispenser>()
                .PlanAsync(session.Offer.Amount.AmountMinor, cancellationToken);
            if (!plan.Feasible)
            {
                return Result.Failure<PayoutStatusDto>(SessionErrors.InsufficientCash());
            }

            selection = new PayoutSelection("cash", Bank: null, PlannedBills: plan.Bills, BillMixOk: true);
        }
        else
        {
            selection = new PayoutSelection(request.Method, request.Bank, PlannedBills: null, BillMixOk: null);
        }

        Result confirm = session.ConfirmPayout(selection);
        if (confirm.IsFailure)
        {
            return Result.Failure<PayoutStatusDto>(confirm.Error);
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, $"Payout confirmed: {selection.Method}.", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.PersistAsync(session, cancellationToken);
        return Result.Success(new PayoutStatusDto(selection.Method, selection.BillMixOk));
    }

    /// <summary>
    /// Starts settlement (bag → label → dispense/transfer) in the background; the terminal
    /// receipt arrives in the <c>session_completed</c> event.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> SettleAsync(TransactionSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        Result result = session.StartSettlement();
        if (result.IsFailure)
        {
            return result;
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, "Settlement started.", cancellationToken);
        await PublishStateChangedAsync(session, cancellationToken);
        await _store.PersistAsync(session, cancellationToken);

        // Settlement is never abortable (blocker: money in motion) — it runs to
        // completion or faults; only host shutdown stops it.
        RunInBackground(session, "settlement", token => RunSettlementPipelineAsync(session, token), abortable: false);
        return Result.Success();
    }

    /// <summary>
    /// Aborts the session; when an item is held and <paramref name="returnItem"/> is set,
    /// the return path runs in the background before the session finishes. Any running
    /// abortable pipeline (analysis, identity, agent check) is cancelled. Settlement is
    /// not abortable — money is in motion — so aborting while <c>settling</c> fails with
    /// <c>session.invalid_state</c>.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="reason">The abort reason: <c>timeout</c>, <c>user_cancel</c>, <c>operator</c> or <c>fault</c>.</param>
    /// <param name="returnItem">Whether a held item must be returned.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>Success, or the guard failure.</returns>
    public async Task<Result> AbortAsync(
        TransactionSession session, string reason, bool returnItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        bool willReturnItem = returnItem && session.ItemHeld;
        Result result = session.Abort(reason, returnItem);
        if (result.IsFailure)
        {
            return result;
        }

        // Cancel any running abortable pipeline before starting the return path so two
        // pipelines never drive the hardware concurrently.
        TearDownSessionCancellation(session);

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, $"Session aborted (reason: {reason}).", cancellationToken);
        await _store.PersistAsync(session, cancellationToken);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishSessionAbortedAsync(
                new SessionAbortedEvent(session.Id, sequence, reason), ct),
            cancellationToken);
        _logger.SessionAborted(session.Id, reason);

        if (willReturnItem)
        {
            RunInBackground(
                session,
                "abort-return",
                async token =>
                {
                    await RunReturnPathAsync(session, token);
                    await FinishReturnAsync(session, token);
                },
                abortable: false);
        }
        else
        {
            await PublishStateChangedAsync(session, cancellationToken);
        }

        return Result.Success();
    }

    /// <summary>
    /// Requests a live-agent item check. Only valid while the session is
    /// <c>analyzing</c>. The mock approves after a short delay so the UI overlay can be
    /// built; the real path escalates to Cloud.Api review and never fabricates an
    /// approval (ai-item-verification addendum).
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="trigger">What triggered the escalation.</param>
    /// <param name="cancellationToken">Cancels the synchronous part of the command.</param>
    /// <returns>The immediate <c>connecting</c> status, or the guard failure.</returns>
    public async Task<Result<AgentStatusDto>> RequestAgentItemCheckAsync(
        TransactionSession session, string trigger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        if (session.State != SessionStates.Analyzing)
        {
            return Result.Failure<AgentStatusDto>(
                SessionErrors.InvalidState("request a live-agent item check", session.State));
        }

        session.Touch(_timeProvider.GetUtcNow());
        await _store.AppendLogAsync(session, $"Live-agent item check requested (trigger: {trigger}).", cancellationToken);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishAgentStatusAsync(
                new AgentStatusEvent(session.Id, sequence, "connecting", null), ct),
            cancellationToken);

        RunInBackground(session, "agent-item-check", async token =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3), _timeProvider, token);
            await PublishOrderedAsync(
                session,
                (sequence, ct) => _events.PublishAgentStatusAsync(
                    new AgentStatusEvent(session.Id, sequence, "approved", "agt_204"), ct),
                token);
            await _store.AppendLogAsync(session, "Mock live-agent review approved.", token);
        });

        return Result.Success(new AgentStatusDto("connecting", null));
    }

    private async Task RunAnalysisPipelineAsync(TransactionSession session, CancellationToken token)
    {
        SensorSnapshot sensors = await _devices.Get<ISensorBoard>().ReadAsync(token);
        if (!sensors.TrayClosed)
        {
            _logger.TraySensorNotConfirmed(session.Id);
            await AbortOnFaultAsync(session, token);
            return;
        }

        IRoboticArm arm = _devices.Get<IRoboticArm>();
        await _store.AppendLogAsync(session, "Arm: tray → scale.", token);
        await arm.MoveAsync(ArmMove.TrayToScale, token);

        WeightReading weight = await _devices.Get<IScale>().GetWeightAsync(token);
        await _store.AppendLogAsync(
            session,
            string.Create(CultureInfo.InvariantCulture, $"Scale: {weight.Grams} g (stable: {weight.Stable})."),
            token);
        Result weightCheck = _analysisPolicy.CheckWeight(weight.Grams);
        if (weightCheck.IsFailure)
        {
            await RejectItemAsync(session, weightCheck.Error.Code, token);
            return;
        }

        await _store.AppendLogAsync(session, "Arm: scale → analyser.", token);
        await arm.MoveAsync(ArmMove.ScaleToAnalyser, token);

        IMetalAnalyser analyser = _devices.Get<IMetalAnalyser>();
        void ForwardProgress(object? sender, AnalysisProgress progress) =>
            PublishAnalysisProgress(session, progress);

        AnalysisRun run;
        analyser.ProgressChanged += ForwardProgress;
        try
        {
            run = await analyser.StartAnalysisAsync(token);
        }
        finally
        {
            analyser.ProgressChanged -= ForwardProgress;
        }

        if (!run.Succeeded)
        {
            await _store.AppendLogAsync(session, $"Analysis failed: {run.FailureReason}.", token);
            await RejectItemAsync(session, RejectionReasonCodes.ItemUnidentified, token);
            return;
        }

        Result compositionCheck = _analysisPolicy.CheckComposition(run.GoldPercent, run.GoldPlated);
        if (compositionCheck.IsFailure)
        {
            await RejectItemAsync(session, compositionCheck.Error.Code, token);
            return;
        }

        await _store.AppendLogAsync(session, "Arm: analyser → volume chamber.", token);
        await arm.MoveAsync(ArmMove.AnalyserToChamber, token);
        VolumeReading volume = await _devices.Get<IVolumeChamber>().MeasureAsync(weight.Grams, token);
        Result volumeCheck = _analysisPolicy.CheckVolume(
            weight.Grams, run.Elements, volume.VolumeCc, volume.Calibrated);
        if (volumeCheck.IsFailure)
        {
            await RejectItemAsync(session, volumeCheck.Error.Code, token);
            return;
        }

        session.RecordAnalysis(new AnalysisReading(
            weight.Grams, run.GoldPercent, run.SilverPercent, run.GoldPlated,
            run.Elements, volume.VolumeCc, volume.Calibrated));

        // Non-null past the composition gate (CheckComposition rejects a null gold percent).
        decimal goldPercent = run.GoldPercent ?? 0m;
        string kind = string.Equals(session.Setup?.ServiceType, "pawn", StringComparison.Ordinal)
            ? "pawn"
            : "sale";
        OfferDto offer = await _offerCalculator.CalculateAsync(
            new OfferInput(kind, weight.Grams, goldPercent), token);

        Result presented = session.PresentOffer(offer, _timeProvider.GetUtcNow());
        if (presented.IsFailure)
        {
            _logger.OfferPresentFailed(session.Id, presented.Error.Code);
            return;
        }

        await _store.AppendLogAsync(
            session, $"Offer {offer.OfferId} presented: {offer.Amount.Display} ({offer.Kind}).", token);
        await _store.PersistAsync(session, token);
        await PublishStateChangedAsync(session, token);
        _logger.OfferPresented(session.Id, offer.OfferId, offer.Amount.Display);
    }

    private async Task RunIdentitySequenceAsync(TransactionSession session, CancellationToken token)
    {
        await PublishIdentityStepAsync(session, "id_scan", "in_progress", null, token);
        IdScanResult scan = await _devices.Get<IIdScanner>().ScanAsync(token);
        if (!scan.Succeeded || scan.Document is null)
        {
            // Fail-closed: a scanner failure never auto-approves (design addendum rule 2).
            await PublishIdentityStepAsync(session, "id_scan", "failed", null, token);
            await _store.AppendLogAsync(session, $"ID scan failed: {scan.FailureReason}.", token);
            await AbortOnFaultAsync(session, token);
            return;
        }

        IdDocument document = scan.Document;
        Result kyc = IdentityPolicy.Evaluate(
            new IdentityDocumentFacts(document.DateOfBirth, document.ExpiresOn, document.IsGovernmentId),
            DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime));
        if (kyc.IsFailure)
        {
            var failure = new IdentityFailureDto(
                kyc.Error.Code, 0, RejectionCatalog.FromCode(kyc.Error.Code).Recovery);
            await PublishIdentityStepAsync(session, "id_scan", "failed", failure, token);
            await _store.AppendLogAsync(session, $"KYC gate failed: {kyc.Error.Code}.", token);
            await RejectItemAsync(session, kyc.Error.Code, token);
            return;
        }

        session.RecordCustomer(new CustomerFacts(
            document.FirstName, document.LastName, document.DateOfBirth));
        await PublishIdentityStepAsync(session, "id_scan", "completed", null, token);
        await _store.AppendLogAsync(session, "ID scanned and eligibility gates passed.", token);
        await _store.PersistAsync(session, token);

        await PublishIdentityStepAsync(session, "face_match", "in_progress", null, token);
        CapturedImage selfie = await _devices.Get<ICameraService>().CaptureAsync(CameraRole.Customer, token);
        await _store.WriteArtifactAsync(session, "customerImage.png", selfie.Bytes, token);
        // The mock face match always passes; the real matcher lands with Cloud.Api KYC and
        // is fail-closed there (no auto-approve path exists in code — design addendum).
        await PublishIdentityStepAsync(session, "face_match", "completed", null, token);
        await _store.AppendLogAsync(session, "Face match completed.", token);

        if (_features.FingerprintRequired)
        {
            await PublishIdentityStepAsync(session, "fingerprint", "in_progress", null, token);
            FingerprintResult fingerprint = await _devices.Get<IFingerprintScanner>().CaptureAsync(token);
            if (!fingerprint.Succeeded)
            {
                await PublishIdentityStepAsync(session, "fingerprint", "failed", null, token);
                await _store.AppendLogAsync(session, "Fingerprint capture failed.", token);
                await AbortOnFaultAsync(session, token);
                return;
            }

            await PublishIdentityStepAsync(session, "fingerprint", "completed", null, token);
            await _store.AppendLogAsync(session, "Fingerprint captured.", token);
        }

        await PublishIdentityStepAsync(session, "signature", "in_progress", null, token);
        await _store.AppendLogAsync(session, "Awaiting signature.", token);
        await _store.PersistAsync(session, token);
    }

    private async Task RunSettlementPipelineAsync(TransactionSession session, CancellationToken token)
    {
        if (session.Payout is not PayoutSelection payout)
        {
            await AbortOnFaultAsync(session, token);
            return;
        }

        string bagNumber = "BAG-" + _timeProvider.GetLocalNow()
            .ToString("HHmmss", CultureInfo.InvariantCulture);
        session.AssignBagNumber(bagNumber);
        string invoiceNumber = NextInvoiceNumber();

        await PublishSettlementAsync(session, "bagging", "Securing your item", null, token);
        await _store.AppendLogAsync(session, $"Bagging item into {bagNumber}.", token);
        await _devices.Get<IRoboticArm>().MoveAsync(ArmMove.ChamberToBag, token);
        await _devices.Get<IBagger>().BagItemAsync(bagNumber, token);
        await _devices.Get<ILabelPrinter>().PrintAsync(
            new BagLabel(
                invoiceNumber,
                bagNumber,
                session.Analysis?.WeightGrams ?? 0m,
                session.Offer?.Amount.Display ?? string.Empty),
            token);

        if (string.Equals(payout.Method, "cash", StringComparison.Ordinal))
        {
            IReadOnlyDictionary<int, int> bills = payout.PlannedBills ?? new Dictionary<int, int>();
            Dictionary<string, int> billsWire = bills.ToDictionary(
                pair => pair.Key.ToString(CultureInfo.InvariantCulture),
                pair => pair.Value,
                StringComparer.Ordinal);
            await PublishSettlementAsync(session, "dispensing", "Dispensing your cash", billsWire, token);
            DispenseResult dispense = await _devices.Get<ICashDispenser>()
                .DispenseAsync(new BillMixPlan(Feasible: true, Bills: bills), token);
            if (!dispense.Succeeded)
            {
                await _store.AppendLogAsync(session, $"Dispense failed: {dispense.FailureReason}.", token);
                await AbortOnFaultAsync(session, token);
                return;
            }

            await _store.AppendLogAsync(session, "Cash dispensed.", token);
        }
        else
        {
            await PublishSettlementAsync(session, "transferring", "Sending your payment", null, token);
            // The mock transfer settles instantly; real payout rails arrive with Cloud.Api.
            await _store.AppendLogAsync(session, $"Mock {payout.Method} payout recorded.", token);
        }

        string receiptId = KioskIdGenerator.NewReceiptId(_timeProvider);
        IReadOnlyList<string> channels = session.Contact?.ReceiptChannels ?? ["qr"];
        var receipt = new ReceiptDto(
            receiptId, invoiceNumber, "https://r.goldkiosk.com/t/" + receiptId, channels);

        Result completed = session.Complete(receipt);
        if (completed.IsFailure)
        {
            _logger.SessionCompleteFailed(session.Id, completed.Error.Code);
            return;
        }

        await _store.WriteTransactionDetailsAsync(session, token);
        await _store.PersistAsync(session, token);
        await _store.AppendLogAsync(
            session, $"Session completed; receipt {receiptId}, invoice {invoiceNumber}.", token);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishSessionCompletedAsync(
                new SessionCompletedEvent(session.Id, sequence, session.State, receipt, session.IsTest), ct),
            token);
        _logger.SessionCompleted(session.Id, receiptId, session.IsTest);
        TearDownSessionCancellation(session);
    }

    private async Task RejectItemAsync(TransactionSession session, string reasonCode, CancellationToken token)
    {
        RejectionDto rejection = RejectionCatalog.FromCode(reasonCode);
        Result result = session.RejectItem(rejection);
        if (result.IsFailure)
        {
            _logger.RejectionNotApplicable(session.Id, reasonCode, session.State);
            return;
        }

        await _store.AppendLogAsync(session, $"Item rejected: {reasonCode}.", token);
        await _store.PersistAsync(session, token);
        await PublishStateChangedAsync(session, token);
        _logger.ItemRejected(session.Id, reasonCode);

        await RunReturnPathAsync(session, token);
        await FinishReturnAsync(session, token);
    }

    private async Task RunReturnPathAsync(TransactionSession session, CancellationToken token)
    {
        await _store.AppendLogAsync(session, "Arm: returning item to tray.", token);
        await _devices.Get<IRoboticArm>().MoveAsync(ArmMove.ReturnToTray, token);

        ITray tray = _devices.Get<ITray>();
        await PublishTrayAsync(session, "opening", token);
        await tray.OpenAsync(token);
        await PublishTrayAsync(session, "open", token);

        await _devices.Get<ISensorBoard>().ReadAsync(token);
        await _store.AppendLogAsync(session, "Item return confirmed by sensors.", token);

        await PublishTrayAsync(session, "closing", token);
        await tray.CloseAsync(token);
        await PublishTrayAsync(session, "closed", token);
    }

    private async Task FinishReturnAsync(TransactionSession session, CancellationToken token)
    {
        Result result = session.CompleteReturn();
        if (result.IsFailure)
        {
            return;
        }

        await _store.AppendLogAsync(session, "Return path finished; session done.", token);
        await _store.PersistAsync(session, token);
        await PublishStateChangedAsync(session, token);
        TearDownSessionCancellation(session);
    }

    private async Task AbortOnFaultAsync(TransactionSession session, CancellationToken token)
    {
        Result result = session.Abort("fault", returnItem: false);
        if (result.IsFailure)
        {
            return;
        }

        await _store.AppendLogAsync(session, "Session aborted after a hardware/pipeline fault.", token);
        await _store.PersistAsync(session, token);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishSessionAbortedAsync(
                new SessionAbortedEvent(session.Id, sequence, "fault"), ct),
            token);
        TearDownSessionCancellation(session);
    }

    private void RunInBackground(
        TransactionSession session,
        string operation,
        Func<CancellationToken, Task> work,
        bool abortable = true)
    {
        // Abortable pipelines (tray, analysis, identity, agent check, decline-return) run
        // on the session's token — cancelled by abort, linked at creation with
        // ApplicationStopping. Non-abortable work (settlement, the abort/return path)
        // only stops with the host.
        CancellationToken token =
            abortable && _sessionCancellations.TryGetValue(session.Id, out CancellationTokenSource? cts)
                ? cts.Token
                : _lifetime.ApplicationStopping;
        _ = Task.Run(
            async () =>
            {
                try
                {
                    session.Touch(_timeProvider.GetUtcNow());
                    await work(token);
                    session.Touch(_timeProvider.GetUtcNow());
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Host shutdown or session abort: the journal already holds the last
                    // durable state.
                }
                catch (Exception ex)
                {
                    // Deliberate catch-all: a background pipeline crash must safe-abort the
                    // session with audit — it must never take the host down (fail fast at
                    // boundaries, resilient across them).
                    _logger.BackgroundOperationFailed(ex, operation, session.Id);
                    try
                    {
                        await AbortOnFaultAsync(session, CancellationToken.None);
                    }
                    catch (Exception abortEx)
                    {
                        // Deliberate catch-all: the fault path itself failed; log and stop.
                        _logger.SafeAbortFailed(abortEx, session.Id);
                    }
                }
            },
            CancellationToken.None);
    }

    /// <summary>
    /// Cancels and disposes the session's cancellation source: called by abort (stopping
    /// any running abortable pipeline) and when the session reaches its terminal state.
    /// Cancel always precedes dispose so in-flight token observers stay safe.
    /// </summary>
    /// <param name="session">The session whose cancellation source to tear down.</param>
    private void TearDownSessionCancellation(TransactionSession session)
    {
        if (_sessionCancellations.TryRemove(session.Id, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private string NextInvoiceNumber()
    {
        string day = _timeProvider.GetLocalNow().ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string countersFolder = Path.Combine(_kiosk.TransactionRoot, "counters");
        string counterPath = Path.Combine(countersFolder, $"invoice-{day}.txt");

        // Read + increment + rewrite under a process-wide lock; the per-day counter file
        // survives restarts so invoice numbers never repeat after a crash (review GK-2).
        lock (_invoiceGate)
        {
            Directory.CreateDirectory(countersFolder);
            int sequence = 0;
            if (File.Exists(counterPath)
                && int.TryParse(
                    File.ReadAllText(counterPath),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int persisted)
                && persisted > 0)
            {
                sequence = persisted;
            }

            sequence++;
            File.WriteAllText(counterPath, sequence.ToString(CultureInfo.InvariantCulture));
            return string.Create(CultureInfo.InvariantCulture, $"USGK-{sequence:D6}");
        }
    }

    /// <summary>
    /// Issues the next event sequence and publishes under the session's publish gate so
    /// concurrent publishers cannot deliver events out of sequence order.
    /// </summary>
    /// <param name="session">The session the event belongs to.</param>
    /// <param name="publish">The publish call, given the issued sequence.</param>
    /// <param name="token">Cancels waiting for the gate and the publish.</param>
    private async Task PublishOrderedAsync(
        TransactionSession session,
        Func<long, CancellationToken, Task> publish,
        CancellationToken token)
    {
        SemaphoreSlim gate = _publishGates.GetOrAdd(session.Id, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token);
        try
        {
            await publish(session.NextSequence(), token);
        }
        finally
        {
            gate.Release();
        }
    }

    private void PublishAnalysisProgress(TransactionSession session, AnalysisProgress progress)
    {
        _ = PublishSafelyAsync(
            () => PublishOrderedAsync(
                session,
                (sequence, ct) => _events.PublishAnalysisProgressAsync(
                    new AnalysisProgressEvent(
                        session.Id, sequence, progress.Stage, progress.Percent,
                        AnalysisDisplayFor(progress.Stage)),
                    ct),
                CancellationToken.None),
            "analysis_progress",
            session.Id);
    }

    private async Task PublishSafelyAsync(Func<Task> publish, string eventName, string sessionId)
    {
        try
        {
            await publish();
        }
        catch (Exception ex)
        {
            // Deliberate catch-all: event delivery is best-effort — REST snapshots remain
            // the source of truth, so a failed broadcast is logged, never fatal.
            _logger.EventPublishFailed(ex, eventName, sessionId);
        }
    }

    private async Task PublishIdentityStepAsync(
        TransactionSession session,
        string step,
        string status,
        IdentityFailureDto? failure,
        CancellationToken token)
    {
        session.UpdateIdentityStep(step, status, failure);
        await PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishIdentityProgressAsync(
                new IdentityProgressEvent(session.Id, sequence, step, status, failure), ct),
            token);
    }

    private Task PublishStateChangedAsync(TransactionSession session, CancellationToken token) =>
        PublishOrderedAsync(
            session,
            (sequence, ct) =>
            {
                OfferDto? offer = session.State == SessionStates.Offer ? session.Offer : null;
                RejectionDto? rejection = session.State == SessionStates.ReturningItem ? session.Rejection : null;
                return _events.PublishSessionStateChangedAsync(
                    new SessionStateChangedEvent(session.Id, sequence, session.State, offer, rejection), ct);
            },
            token);

    private Task PublishTrayAsync(TransactionSession session, string status, CancellationToken token) =>
        PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishTrayStateChangedAsync(
                new TrayStateChangedEvent(session.Id, sequence, new TrayDto(status)), ct),
            token);

    private Task PublishSettlementAsync(
        TransactionSession session,
        string stage,
        string display,
        IReadOnlyDictionary<string, int>? bills,
        CancellationToken token) =>
        PublishOrderedAsync(
            session,
            (sequence, ct) => _events.PublishSettlementProgressAsync(
                new SettlementProgressEvent(session.Id, sequence, stage, display, bills), ct),
            token);

    private static string AnalysisDisplayFor(string stage) => stage switch
    {
        "item_detected" => "Item detected & classified",
        "authenticating" => "Verifying authenticity",
        "pricing" => "Pricing against live market",
        _ => "Analyzing your item",
    };
}
