using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Settlement;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.Core.Analysis;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// The crash-safe snapshot written to <c>journal.json</c> in the transaction folder on
/// every transition (ADR 0002). Restricted data is excluded by construction: no bank
/// account details, no ID-document numbers, no customer name/DOB, no email/phone,
/// no biometrics — only the receipt-channel selection survives from contact capture.
/// </summary>
public sealed record SessionJournal
{
    /// <summary>The session identifier.</summary>
    public required string SessionId { get; init; }

    /// <summary>The session state at the time of the write.</summary>
    public required string State { get; init; }

    /// <summary>The last event sequence issued.</summary>
    public required long Sequence { get; init; }

    /// <summary>Whether any participating device was mocked (test transaction, ADR 0004).</summary>
    public required bool IsTest { get; init; }

    /// <summary>When the session began.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>The last customer/UI activity.</summary>
    public required DateTimeOffset LastActivityAt { get; init; }

    /// <summary>The clubbed pre-tray selections, once the tray opened.</summary>
    public SetupDto? Setup { get; init; }

    /// <summary>Whether the customer's item is physically inside the machine.</summary>
    public bool ItemHeld { get; init; }

    /// <summary>The measured item facts, once analysis ran.</summary>
    public AnalysisReading? Analysis { get; init; }

    /// <summary>The presented offer, once made.</summary>
    public OfferDto? Offer { get; init; }

    /// <summary>When the offer was made.</summary>
    public DateTimeOffset? OfferMadeAt { get; init; }

    /// <summary>When the customer actioned the offer.</summary>
    public DateTimeOffset? OfferActionedAt { get; init; }

    /// <summary>The identity checklist steps, once identity started.</summary>
    public IReadOnlyList<IdentityStepDto> IdentitySteps { get; init; } = [];

    /// <summary>The identity step in progress, when identity has started.</summary>
    public string? CurrentIdentityStep { get; init; }

    /// <summary>The terms version the captured signature applies to.</summary>
    public string? SignedTermsVersion { get; init; }

    /// <summary>
    /// The receipt channels chosen at contact capture. Email and phone are deliberately
    /// not journaled (PII stays in memory only).
    /// </summary>
    public IReadOnlyList<string>? ReceiptChannels { get; init; }

    /// <summary>The confirmed payout method.</summary>
    public string? PayoutMethod { get; init; }

    /// <summary>Whether the cassettes could cover a cash payout.</summary>
    public bool? BillMixOk { get; init; }

    /// <summary>The planned bill mix (denomination → count) for a cash payout.</summary>
    public IReadOnlyDictionary<int, int>? PlannedBills { get; init; }

    /// <summary>The physical bag identifier, once assigned.</summary>
    public string? BagNumber { get; init; }

    /// <summary>The terminal receipt, once settlement completed.</summary>
    public ReceiptDto? Receipt { get; init; }

    /// <summary>The rejection details, on the rejection path.</summary>
    public RejectionDto? Rejection { get; init; }

    /// <summary>The abort reason, when the session was aborted.</summary>
    public string? AbortReason { get; init; }

    /// <summary>Idempotency keys processed for this session, key → operation.</summary>
    public IReadOnlyDictionary<string, string> IdempotencyKeys { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Builds the journal snapshot for a session.</summary>
    /// <param name="session">The session to snapshot.</param>
    /// <returns>The journal record ready to serialize.</returns>
    public static SessionJournal FromSession(TransactionSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new SessionJournal
        {
            SessionId = session.Id,
            State = session.State,
            Sequence = session.Sequence,
            IsTest = session.IsTest,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            Setup = session.Setup,
            ItemHeld = session.ItemHeld,
            Analysis = session.Analysis,
            Offer = session.Offer,
            OfferMadeAt = session.OfferMadeAt,
            OfferActionedAt = session.OfferActionedAt,
            IdentitySteps = [.. session.IdentitySteps],
            CurrentIdentityStep = session.CurrentIdentityStep,
            SignedTermsVersion = session.SignedTermsVersion,
            ReceiptChannels = session.Contact?.ReceiptChannels,
            PayoutMethod = session.Payout?.Method,
            BillMixOk = session.Payout?.BillMixOk,
            PlannedBills = session.Payout?.PlannedBills,
            BagNumber = session.BagNumber,
            Receipt = session.Receipt,
            Rejection = session.Rejection,
            AbortReason = session.AbortReason,
            IdempotencyKeys = session.IdempotencyKeys,
        };
    }
}
