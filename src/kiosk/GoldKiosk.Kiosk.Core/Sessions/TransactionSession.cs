using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Contact;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Settlement;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Analysis;

namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// The edge-local transaction session aggregate: a guarded state machine over the wire
/// states in <see cref="SessionStates"/> (design note §3) with a per-session monotonic
/// event sequence, the data collected along the flow, and the offer-lock TTL check.
/// Invalid transitions surface as <c>session.invalid_state</c> failures, never exceptions.
/// </summary>
public sealed class TransactionSession
{
    private static readonly Dictionary<string, string[]> _allowedTransitions = new(StringComparer.Ordinal)
    {
        [SessionStates.Attract] = [SessionStates.Welcome],
        [SessionStates.Welcome] = [SessionStates.PlacingItem],
        [SessionStates.PlacingItem] = [SessionStates.Welcome, SessionStates.Analyzing],
        [SessionStates.Analyzing] = [SessionStates.Offer, SessionStates.ReturningItem],
        [SessionStates.Offer] = [SessionStates.Identity, SessionStates.ReturningItem],
        [SessionStates.Identity] = [SessionStates.Contact, SessionStates.ReturningItem],
        [SessionStates.Contact] = [SessionStates.Payout, SessionStates.ReturningItem],
        [SessionStates.Payout] = [SessionStates.PayoutConfirmed, SessionStates.ReturningItem],
        [SessionStates.PayoutConfirmed] = [SessionStates.Settling, SessionStates.ReturningItem],
        [SessionStates.Settling] = [SessionStates.Done, SessionStates.ReturningItem],
        [SessionStates.ReturningItem] = [SessionStates.Done],
        [SessionStates.Done] = [],
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<string, string> _idempotencyKeys = new(StringComparer.Ordinal);
    private readonly List<IdentityStepDto> _identitySteps = [];
    private long _sequence;
    private long _lastActivityTicks;

    private TransactionSession(string id, bool isTest, DateTimeOffset createdAt)
    {
        Id = id;
        IsTest = isTest;
        CreatedAt = createdAt;
        _lastActivityTicks = createdAt.UtcTicks;
        State = SessionStates.Welcome;
    }

    /// <summary>The session identifier, e.g. <c>ses_01JZC…</c>.</summary>
    public string Id { get; }

    /// <summary>The current wire state (see <see cref="SessionStates"/>).</summary>
    public string State { get; private set; }

    /// <summary>
    /// Whether any device served by a simulator participated — sessions with a mocked
    /// device are flagged as test transactions (ADR 0004).
    /// </summary>
    public bool IsTest { get; }

    /// <summary>When the session began.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// The last customer/UI activity, used by the idle-timeout watchdog. Backed by UTC
    /// ticks via <see cref="Interlocked"/> so the watchdog reads it tear-free without
    /// taking the session gate.
    /// </summary>
    public DateTimeOffset LastActivityAt => new(Interlocked.Read(ref _lastActivityTicks), TimeSpan.Zero);

    /// <summary>The last event sequence issued for this session.</summary>
    public long Sequence => Interlocked.Read(ref _sequence);

    /// <summary>The per-transaction folder path once created at tray open (ADR 0002).</summary>
    public string? TransactionFolder { get; private set; }

    /// <summary>The clubbed pre-tray selections (service type, locale, terms).</summary>
    public SetupDto? Setup { get; private set; }

    /// <summary>Whether the customer's item is physically inside the machine.</summary>
    public bool ItemHeld { get; private set; }

    /// <summary>The measured item facts after the analysis pipeline ran.</summary>
    public AnalysisReading? Analysis { get; private set; }

    /// <summary>The offer presented to the customer, once made.</summary>
    public OfferDto? Offer { get; private set; }

    /// <summary>When the offer was made (starts the lock TTL).</summary>
    public DateTimeOffset? OfferMadeAt { get; private set; }

    /// <summary>When the customer accepted or declined the offer.</summary>
    public DateTimeOffset? OfferActionedAt { get; private set; }

    /// <summary>The identity checklist steps, once the identity sequence started.</summary>
    public IReadOnlyList<IdentityStepDto> IdentitySteps
    {
        get
        {
            lock (_gate)
            {
                return [.. _identitySteps];
            }
        }
    }

    /// <summary>The identity step currently in progress, when identity has started.</summary>
    public string? CurrentIdentityStep { get; private set; }

    /// <summary>Customer facts from the ID scan; in-memory only, never journaled.</summary>
    public CustomerFacts? Customer { get; private set; }

    /// <summary>The terms version the captured signature applies to.</summary>
    public string? SignedTermsVersion { get; private set; }

    /// <summary>The clubbed contact and receipt-channel capture.</summary>
    public ContactRequest? Contact { get; private set; }

    /// <summary>The confirmed payout selection.</summary>
    public PayoutSelection? Payout { get; private set; }

    /// <summary>The physical bag identifier assigned at settlement.</summary>
    public string? BagNumber { get; private set; }

    /// <summary>The terminal receipt, once settlement completed.</summary>
    public ReceiptDto? Receipt { get; private set; }

    /// <summary>Why the item/customer was rejected, on the rejection path.</summary>
    public RejectionDto? Rejection { get; private set; }

    /// <summary>The abort reason (<c>timeout</c>, <c>user_cancel</c>, <c>operator</c>, <c>fault</c>), when aborted.</summary>
    public string? AbortReason { get; private set; }

    /// <summary>Whether the session reached its terminal state.</summary>
    public bool IsTerminal => State == SessionStates.Done;

    /// <summary>Idempotency keys processed for this session, key → operation.</summary>
    public IReadOnlyDictionary<string, string> IdempotencyKeys
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, string>(_idempotencyKeys, StringComparer.Ordinal);
            }
        }
    }

    /// <summary>Begins a new session in the <c>welcome</c> state (sequence 1).</summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="isTest">Whether any participating device is mocked.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The new session.</returns>
    public static TransactionSession Begin(string id, bool isTest, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var session = new TransactionSession(id, isTest, now);
        session.NextSequence();
        return session;
    }

    /// <summary>Issues the next per-session monotonic event sequence number.</summary>
    /// <returns>The new sequence value.</returns>
    public long NextSequence() => Interlocked.Increment(ref _sequence);

    /// <summary>Records customer/UI activity for the idle-timeout watchdog.</summary>
    /// <param name="now">The current time.</param>
    public void Touch(DateTimeOffset now) => Interlocked.Exchange(ref _lastActivityTicks, now.UtcTicks);

    /// <summary>Applies the tray-open command: <c>welcome → placing_item</c>, storing the clubbed setup.</summary>
    /// <param name="setup">The clubbed pre-tray selections.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result OpenTray(SetupDto setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        lock (_gate)
        {
            Result result = TransitionCore(SessionStates.PlacingItem, "open the tray");
            if (result.IsSuccess)
            {
                Setup = setup;
            }

            return result;
        }
    }

    /// <summary>
    /// Applies the tray-close command: with an item <c>placing_item → analyzing</c>;
    /// without one (customer backed out) <c>placing_item → welcome</c>.
    /// </summary>
    /// <param name="hasItem">Whether the customer placed an item.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result CloseTray(bool hasItem)
    {
        lock (_gate)
        {
            Result result = TransitionCore(
                hasItem ? SessionStates.Analyzing : SessionStates.Welcome,
                "close the tray");
            if (result.IsSuccess)
            {
                ItemHeld = hasItem;
            }

            return result;
        }
    }

    /// <summary>Records the measured item facts from the analysis pipeline.</summary>
    /// <param name="analysis">The measured item facts.</param>
    public void RecordAnalysis(AnalysisReading analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        lock (_gate)
        {
            Analysis = analysis;
        }
    }

    /// <summary>Presents the offer: <c>analyzing → offer</c>, starting the lock TTL.</summary>
    /// <param name="offer">The offer to present.</param>
    /// <param name="now">The current time (recorded as the offer-made instant).</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result PresentOffer(OfferDto offer, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(offer);
        lock (_gate)
        {
            Result result = TransitionCore(SessionStates.Offer, "present an offer");
            if (result.IsSuccess)
            {
                Offer = offer;
                OfferMadeAt = now;
            }

            return result;
        }
    }

    /// <summary>Rejects the item: <c>analyzing → returning_item</c> with the rejection details.</summary>
    /// <param name="rejection">Why the item was rejected.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result RejectItem(RejectionDto rejection)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        lock (_gate)
        {
            Result result = TransitionCore(SessionStates.ReturningItem, "reject the item");
            if (result.IsSuccess)
            {
                Rejection = rejection;
            }

            return result;
        }
    }

    /// <summary>
    /// Accepts the offer: <c>offer → identity</c>. Fails with <c>offer.expired</c> past the
    /// lock TTL and <c>offer.already_actioned</c> on a second action.
    /// </summary>
    /// <param name="now">The current time, compared against the offer expiry.</param>
    /// <returns>Success, or the specific offer failure.</returns>
    public Result AcceptOffer(DateTimeOffset now) => ActionOffer(SessionStates.Identity, "accept the offer", now);

    /// <summary>Declines the offer: <c>offer → returning_item</c>. Same guards as accept.</summary>
    /// <param name="now">The current time, compared against the offer expiry.</param>
    /// <returns>Success, or the specific offer failure.</returns>
    public Result DeclineOffer(DateTimeOffset now) => ActionOffer(SessionStates.ReturningItem, "decline the offer", now);

    /// <summary>
    /// Starts the identity checklist in the <c>identity</c> state:
    /// <c>id_scan → face_match → (fingerprint) → signature</c>.
    /// </summary>
    /// <param name="fingerprintRequired">Whether the fingerprint step is configured on.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result StartIdentity(bool fingerprintRequired)
    {
        lock (_gate)
        {
            if (State != SessionStates.Identity)
            {
                return Result.Failure(SessionErrors.InvalidState("start identity", State));
            }

            if (_identitySteps.Count > 0)
            {
                return Result.Failure(SessionErrors.InvalidState("restart identity", State));
            }

            _identitySteps.Add(new IdentityStepDto("id_scan", "pending", null));
            _identitySteps.Add(new IdentityStepDto("face_match", "pending", null));
            if (fingerprintRequired)
            {
                _identitySteps.Add(new IdentityStepDto("fingerprint", "pending", null));
            }

            _identitySteps.Add(new IdentityStepDto("signature", "pending", null));
            CurrentIdentityStep = "id_scan";
            return Result.Success();
        }
    }

    /// <summary>Updates one identity step's status and moves the current-step pointer.</summary>
    /// <param name="step">The step name, e.g. <c>id_scan</c>.</param>
    /// <param name="status">The new status, e.g. <c>in_progress</c>, <c>completed</c>, <c>failed</c>.</param>
    /// <param name="failure">The failure details when <paramref name="status"/> is <c>failed</c>.</param>
    public void UpdateIdentityStep(string step, string status, IdentityFailureDto? failure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        lock (_gate)
        {
            int index = _identitySteps.FindIndex(s => s.Step == step);
            if (index >= 0)
            {
                _identitySteps[index] = new IdentityStepDto(step, status, failure);
                CurrentIdentityStep = step;
            }
        }
    }

    /// <summary>Records the customer facts extracted from the ID scan (in-memory only).</summary>
    /// <param name="customer">The customer facts.</param>
    public void RecordCustomer(CustomerFacts customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        lock (_gate)
        {
            Customer = customer;
        }
    }

    /// <summary>
    /// Completes the signature step: <c>identity → contact</c>. Requires the identity
    /// pipeline to have actually reached the signature step with a scanned customer on
    /// record — a signature posted straight after offer acceptance (skipping the KYC
    /// gates) fails with <c>session.invalid_state</c>.
    /// </summary>
    /// <param name="signedTermsVersion">The terms version the signature applies to.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result CompleteSignature(string signedTermsVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedTermsVersion);
        lock (_gate)
        {
            if (!string.Equals(CurrentIdentityStep, "signature", StringComparison.Ordinal) || Customer is null)
            {
                return Result.Failure(SessionErrors.InvalidState("submit the signature", State));
            }

            Result result = TransitionCore(SessionStates.Contact, "submit the signature");
            if (result.IsSuccess)
            {
                SignedTermsVersion = signedTermsVersion;
                UpdateIdentityStepUnsafe("signature", "completed");
            }

            return result;
        }
    }

    /// <summary>Stores the clubbed contact capture: <c>contact → payout</c>.</summary>
    /// <param name="contact">The contact and receipt-channel selections.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result SetContact(ContactRequest contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        lock (_gate)
        {
            Result result = TransitionCore(SessionStates.Payout, "submit contact details");
            if (result.IsSuccess)
            {
                Contact = contact;
            }

            return result;
        }
    }

    /// <summary>Confirms the payout method: <c>payout → payout_confirmed</c>.</summary>
    /// <param name="payout">The confirmed payout selection.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result ConfirmPayout(PayoutSelection payout)
    {
        ArgumentNullException.ThrowIfNull(payout);
        lock (_gate)
        {
            Result result = TransitionCore(SessionStates.PayoutConfirmed, "confirm the payout");
            if (result.IsSuccess)
            {
                Payout = payout;
            }

            return result;
        }
    }

    /// <summary>Starts settlement: <c>payout_confirmed → settling</c>.</summary>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result StartSettlement()
    {
        lock (_gate)
        {
            return TransitionCore(SessionStates.Settling, "settle");
        }
    }

    /// <summary>Assigns the physical bag identifier at settlement.</summary>
    /// <param name="bagNumber">The bag identifier printed on the label.</param>
    public void AssignBagNumber(string bagNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bagNumber);
        lock (_gate)
        {
            BagNumber = bagNumber;
        }
    }

    /// <summary>
    /// Completes the session: <c>settling → done</c> with the terminal receipt. Only valid
    /// from <c>settling</c> — the return path finishes via <see cref="CompleteReturn"/>.
    /// </summary>
    /// <param name="receipt">The receipt for the completed transaction.</param>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result Complete(ReceiptDto receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        lock (_gate)
        {
            if (State != SessionStates.Settling)
            {
                return Result.Failure(SessionErrors.InvalidState("complete the session", State));
            }

            Result result = TransitionCore(SessionStates.Done, "complete the session");
            if (result.IsSuccess)
            {
                Receipt = receipt;
                ItemHeld = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Finishes the return path: <c>returning_item → done</c>. Only valid from
    /// <c>returning_item</c> — settlement finishes via <see cref="Complete"/>.
    /// </summary>
    /// <returns>Success, or <c>session.invalid_state</c>.</returns>
    public Result CompleteReturn()
    {
        lock (_gate)
        {
            if (State != SessionStates.ReturningItem)
            {
                return Result.Failure(SessionErrors.InvalidState("finish returning the item", State));
            }

            Result result = TransitionCore(SessionStates.Done, "finish returning the item");
            if (result.IsSuccess)
            {
                ItemHeld = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Aborts the session from any non-terminal state except <c>settling</c> — the one
    /// sanctioned escape from the transition map. While settling, money is in motion
    /// (bagging, dispensing, transferring) and no safe abort exists, so the request fails
    /// with <c>session.invalid_state</c>. Moves to <c>returning_item</c> when an item is
    /// held and must go back, otherwise straight to <c>done</c>.
    /// </summary>
    /// <param name="reason">The abort reason: <c>timeout</c>, <c>user_cancel</c>, <c>operator</c> or <c>fault</c>.</param>
    /// <param name="returnItem">Whether a held item must be returned to the customer.</param>
    /// <returns>Success, or <c>session.invalid_state</c> when terminal or settling.</returns>
    public Result Abort(string reason, bool returnItem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        lock (_gate)
        {
            if (IsTerminal || State == SessionStates.Settling)
            {
                return Result.Failure(SessionErrors.InvalidState("abort", State));
            }

            AbortReason = reason;
            State = returnItem && ItemHeld ? SessionStates.ReturningItem : SessionStates.Done;
            return Result.Success();
        }
    }

    /// <summary>Records the per-transaction folder assigned by the session store.</summary>
    /// <param name="folderPath">The absolute folder path.</param>
    public void AttachFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        lock (_gate)
        {
            TransactionFolder = folderPath;
        }
    }

    /// <summary>
    /// Records an idempotency key. Returns <see langword="false"/> with the original
    /// operation when the key was already processed.
    /// </summary>
    /// <param name="key">The <c>Idempotency-Key</c> header value.</param>
    /// <param name="operation">The operation being keyed, e.g. <c>tray/open</c>.</param>
    /// <param name="existingOperation">The operation the key was first used for, when replayed.</param>
    /// <returns><see langword="true"/> when the key is new.</returns>
    public bool TryRecordIdempotencyKey(string key, string operation, out string? existingOperation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        lock (_gate)
        {
            if (_idempotencyKeys.TryGetValue(key, out string? existing))
            {
                existingOperation = existing;
                return false;
            }

            _idempotencyKeys[key] = operation;
            existingOperation = null;
            return true;
        }
    }

    /// <summary>Checks the offer lock: fails with <c>offer.expired</c> once the TTL elapsed.</summary>
    /// <param name="now">The current time.</param>
    /// <returns>Success while the lock holds.</returns>
    public Result EnsureOfferActive(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (Offer is null)
            {
                return Result.Failure(SessionErrors.InvalidState("act on an offer", State));
            }

            return now >= Offer.ExpiresAt
                ? Result.Failure(SessionErrors.OfferExpired())
                : Result.Success();
        }
    }

    private Result ActionOffer(string targetState, string action, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (State != SessionStates.Offer || Offer is null)
            {
                return Result.Failure(SessionErrors.InvalidState(action, State));
            }

            if (OfferActionedAt is not null)
            {
                return Result.Failure(SessionErrors.OfferAlreadyActioned());
            }

            if (now >= Offer.ExpiresAt)
            {
                return Result.Failure(SessionErrors.OfferExpired());
            }

            Result result = TransitionCore(targetState, action);
            if (result.IsSuccess)
            {
                OfferActionedAt = now;
            }

            return result;
        }
    }

    private Result TransitionCore(string targetState, string action)
    {
        if (!_allowedTransitions.TryGetValue(State, out string[]? targets)
            || !targets.Contains(targetState, StringComparer.Ordinal))
        {
            return Result.Failure(SessionErrors.InvalidState(action, State));
        }

        State = targetState;
        return Result.Success();
    }

    private void UpdateIdentityStepUnsafe(string step, string status)
    {
        int index = _identitySteps.FindIndex(s => s.Step == step);
        if (index >= 0)
        {
            _identitySteps[index] = new IdentityStepDto(step, status, null);
            CurrentIdentityStep = step;
        }
    }
}
