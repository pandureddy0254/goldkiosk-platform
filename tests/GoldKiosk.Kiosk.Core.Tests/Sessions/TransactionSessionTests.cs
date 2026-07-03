using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Sessions;

[TestFixture]
public sealed class TransactionSessionTests
{
    private static readonly DateTimeOffset Now = SessionDriver.Now;

    private static readonly string[] AllStates =
    [
        SessionStates.Welcome,
        SessionStates.PlacingItem,
        SessionStates.Analyzing,
        SessionStates.Offer,
        SessionStates.Identity,
        SessionStates.Contact,
        SessionStates.Payout,
        SessionStates.PayoutConfirmed,
        SessionStates.Settling,
        SessionStates.ReturningItem,
        SessionStates.Done,
    ];

    private static readonly SessionCommand[] Commands =
    [
        new(
            "open_tray",
            [SessionStates.Welcome],
            SessionStates.PlacingItem,
            s => s.OpenTray(SessionMother.Setup())),
        new(
            "close_tray_with_item",
            [SessionStates.PlacingItem],
            SessionStates.Analyzing,
            s => s.CloseTray(hasItem: true)),
        new(
            "close_tray_without_item",
            [SessionStates.PlacingItem],
            SessionStates.Welcome,
            s => s.CloseTray(hasItem: false)),
        new(
            "present_offer",
            [SessionStates.Analyzing],
            SessionStates.Offer,
            s => s.PresentOffer(SessionMother.Offer(Now.AddMinutes(10)), Now)),
        new(
            "reject_item",
            [
                SessionStates.Analyzing, SessionStates.Offer, SessionStates.Identity, SessionStates.Contact,
                SessionStates.Payout, SessionStates.PayoutConfirmed, SessionStates.Settling,
            ],
            SessionStates.ReturningItem,
            s => s.RejectItem(SessionMother.Rejection())),
        new(
            "accept_offer",
            [SessionStates.Offer],
            SessionStates.Identity,
            s => s.AcceptOffer(Now)),
        new(
            "decline_offer",
            [SessionStates.Offer],
            SessionStates.ReturningItem,
            s => s.DeclineOffer(Now)),
        new(
            "start_identity",
            [SessionStates.Identity],
            SessionStates.Identity,
            s => s.StartIdentity(fingerprintRequired: false)),
        new(
            "complete_signature",
            [],
            SessionStates.Contact,
            s => s.CompleteSignature(SessionMother.TermsVersion)),
        new(
            "set_contact",
            [SessionStates.Contact],
            SessionStates.Payout,
            s => s.SetContact(SessionMother.Contact())),
        new(
            "confirm_payout",
            [SessionStates.Payout],
            SessionStates.PayoutConfirmed,
            s => s.ConfirmPayout(SessionMother.CashPayout())),
        new(
            "start_settlement",
            [SessionStates.PayoutConfirmed],
            SessionStates.Settling,
            s => s.StartSettlement()),
        new(
            "complete",
            [SessionStates.Settling],
            SessionStates.Done,
            s => s.Complete(SessionMother.Receipt())),
        new(
            "complete_return",
            [SessionStates.ReturningItem],
            SessionStates.Done,
            s => s.CompleteReturn()),
        new(
            "abort",
            [
                SessionStates.Welcome, SessionStates.PlacingItem, SessionStates.Analyzing, SessionStates.Offer,
                SessionStates.Identity, SessionStates.Contact, SessionStates.Payout,
                SessionStates.PayoutConfirmed, SessionStates.ReturningItem,
            ],
            SessionStates.Done,
            s => s.Abort("user_cancel", returnItem: false)),
    ];

    private static IEnumerable<TestCaseData> LegalTransitionCases() =>
        Commands.SelectMany(command => command.LegalFromStates.Select(state =>
            new TestCaseData(state, command).SetName($"{{m}}(from {state}, {command.Name})")));

    private static IEnumerable<TestCaseData> IllegalTransitionCases() =>
        Commands.SelectMany(command => AllStates.Except(command.LegalFromStates, StringComparer.Ordinal)
            .Select(state => new TestCaseData(state, command).SetName($"{{m}}(from {state}, {command.Name})")));

    [TestCaseSource(nameof(LegalTransitionCases))]
    public void Transitions_FromAllowedState_SucceedAndLandOnTargetState(string fromState, SessionCommand command)
    {
        TransactionSession session = SessionDriver.InState(fromState);

        Result result = command.Execute(session);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(command.TargetState);
    }

    [TestCaseSource(nameof(IllegalTransitionCases))]
    public void Transitions_FromDisallowedState_FailWithInvalidStateAndLeaveStateUnchanged(
        string fromState, SessionCommand command)
    {
        TransactionSession session = SessionDriver.InState(fromState);

        Result result = command.Execute(session);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
        session.State.Should().Be(fromState);
    }

    [Test]
    public void Begin_NewSession_StartsInWelcomeAtSequenceOne()
    {
        var session = TransactionSession.Begin("ses_TEST", isTest: true, Now);

        session.State.Should().Be(SessionStates.Welcome);
        session.Sequence.Should().Be(1);
        session.IsTest.Should().BeTrue();
        session.CreatedAt.Should().Be(Now);
        session.LastActivityAt.Should().Be(Now);
        session.IsTerminal.Should().BeFalse();
    }

    [Test]
    public void Begin_BlankId_Throws()
    {
        FluentActions.Invoking(() => TransactionSession.Begin(" ", isTest: false, Now))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void NextSequence_EachCall_IncrementsMonotonically()
    {
        TransactionSession session = SessionDriver.Begin();

        long second = session.NextSequence();
        long third = session.NextSequence();

        second.Should().Be(2);
        third.Should().Be(3);
        session.Sequence.Should().Be(3);
    }

    [Test]
    public void Touch_LaterInstant_MovesLastActivityForward()
    {
        TransactionSession session = SessionDriver.Begin();

        session.Touch(Now.AddSeconds(42));

        session.LastActivityAt.Should().Be(Now.AddSeconds(42));
    }

    [Test]
    public void OpenTray_FromWelcome_StoresTheClubbedSetup()
    {
        TransactionSession session = SessionDriver.Begin();

        session.OpenTray(SessionMother.Setup("pawn"));

        session.Setup!.ServiceType.Should().Be("pawn");
    }

    [Test]
    public void CloseTray_WithItem_MarksItemHeld()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        session.CloseTray(hasItem: true);

        session.ItemHeld.Should().BeTrue();
    }

    [Test]
    public void CloseTray_WithoutItem_ReturnsToWelcomeWithoutItem()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        session.CloseTray(hasItem: false);

        session.State.Should().Be(SessionStates.Welcome);
        session.ItemHeld.Should().BeFalse();
    }

    [Test]
    public void PresentOffer_FromAnalyzing_RecordsOfferAndLockStart()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        var offer = SessionMother.Offer(Now.AddMinutes(10));

        session.PresentOffer(offer, Now);

        session.Offer.Should().Be(offer);
        session.OfferMadeAt.Should().Be(Now);
        session.OfferActionedAt.Should().BeNull();
    }

    [Test]
    public void AcceptOffer_JustBeforeExpiry_Succeeds()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        session.PresentOffer(SessionMother.Offer(Now.AddMinutes(10)), Now);

        Result result = session.AcceptOffer(Now.AddMinutes(10).AddTicks(-1));

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Identity);
        session.OfferActionedAt.Should().Be(Now.AddMinutes(10).AddTicks(-1));
    }

    [Test]
    public void AcceptOffer_AtExactExpiryInstant_FailsWithOfferExpired()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        session.PresentOffer(SessionMother.Offer(Now.AddMinutes(10)), Now);

        Result result = session.AcceptOffer(Now.AddMinutes(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offer.expired");
        session.State.Should().Be(SessionStates.Offer);
        session.OfferActionedAt.Should().BeNull();
    }

    [Test]
    public void DeclineOffer_PastExpiry_FailsWithOfferExpired()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        session.PresentOffer(SessionMother.Offer(Now.AddMinutes(10)), Now);

        Result result = session.DeclineOffer(Now.AddMinutes(11));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offer.expired");
    }

    [Test]
    public void AcceptOffer_WhenOfferAlreadyAccepted_FailsWithoutActioningTwice()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Offer);
        DateTimeOffset firstActionAt = Now.AddSeconds(5);
        session.AcceptOffer(firstActionAt);

        Result second = session.AcceptOffer(Now.AddSeconds(6));

        // The state guard fires before the already-actioned guard, so the double-accept
        // surfaces as session.invalid_state; offer.already_actioned is unreachable from the
        // public flow (reported as a production finding).
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("session.invalid_state");
        session.State.Should().Be(SessionStates.Identity);
        session.OfferActionedAt.Should().Be(firstActionAt);
    }

    [Test]
    public void EnsureOfferActive_BeforeAnyOffer_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);

        Result result = session.EnsureOfferActive(Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
    }

    [Test]
    public void EnsureOfferActive_WhileLockHolds_Succeeds()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Offer);

        Result result = session.EnsureOfferActive(Now.AddMinutes(9));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void EnsureOfferActive_AtExpiryInstant_FailsWithOfferExpired()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Offer);

        Result result = session.EnsureOfferActive(Now.AddMinutes(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offer.expired");
    }

    [Test]
    public void CompleteSignature_StraightAfterAcceptOffer_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);

        Result result = session.CompleteSignature(SessionMother.TermsVersion);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
        session.State.Should().Be(SessionStates.Identity);
        session.SignedTermsVersion.Should().BeNull();
    }

    [Test]
    public void CompleteSignature_AtSignatureStepWithoutScannedCustomer_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);
        session.StartIdentity(fingerprintRequired: false);
        session.UpdateIdentityStep("signature", "in_progress");

        Result result = session.CompleteSignature(SessionMother.TermsVersion);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
    }

    [Test]
    public void CompleteSignature_WhenPipelineReachedSignature_MovesToContactAndRecordsTerms()
    {
        TransactionSession session = SessionDriver.AtSignatureStep();

        Result result = session.CompleteSignature(SessionMother.TermsVersion);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Contact);
        session.SignedTermsVersion.Should().Be(SessionMother.TermsVersion);
        session.IdentitySteps.Should().Contain(s => s.Step == "signature" && s.Status == "completed");
    }

    [Test]
    public void StartIdentity_WithoutFingerprint_BuildsThreeStepChecklist()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);

        session.StartIdentity(fingerprintRequired: false);

        session.IdentitySteps.Select(s => s.Step).Should().Equal("id_scan", "face_match", "signature");
        session.CurrentIdentityStep.Should().Be("id_scan");
    }

    [Test]
    public void StartIdentity_WithFingerprint_IncludesFingerprintStep()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);

        session.StartIdentity(fingerprintRequired: true);

        session.IdentitySteps.Select(s => s.Step)
            .Should().Equal("id_scan", "face_match", "fingerprint", "signature");
    }

    [Test]
    public void StartIdentity_SecondCall_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);
        session.StartIdentity(fingerprintRequired: false);

        Result second = session.StartIdentity(fingerprintRequired: false);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("session.invalid_state");
    }

    [Test]
    public void UpdateIdentityStep_KnownStep_UpdatesStatusAndPointer()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);
        session.StartIdentity(fingerprintRequired: false);

        session.UpdateIdentityStep("face_match", "in_progress");

        session.CurrentIdentityStep.Should().Be("face_match");
        session.IdentitySteps.Should().Contain(s => s.Step == "face_match" && s.Status == "in_progress");
    }

    [Test]
    public void UpdateIdentityStep_UnknownStep_LeavesChecklistUnchanged()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Identity);
        session.StartIdentity(fingerprintRequired: false);

        session.UpdateIdentityStep("voice_print", "completed");

        session.CurrentIdentityStep.Should().Be("id_scan");
        session.IdentitySteps.Should().NotContain(s => s.Step == "voice_print");
    }

    [Test]
    public void Abort_WithHeldItemAndReturnRequested_MovesToReturningItem()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);

        Result result = session.Abort("timeout", returnItem: true);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.ReturningItem);
        session.AbortReason.Should().Be("timeout");
    }

    [Test]
    public void Abort_WithHeldItemButNoReturnRequested_GoesStraightToDone()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);

        Result result = session.Abort("fault", returnItem: false);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Done);
    }

    [Test]
    public void Abort_WithoutHeldItem_GoesToDoneEvenWhenReturnRequested()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Welcome);

        Result result = session.Abort("user_cancel", returnItem: true);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Done);
    }

    [Test]
    public void Abort_WhileSettling_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Settling);

        Result result = session.Abort("user_cancel", returnItem: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
        session.State.Should().Be(SessionStates.Settling);
        session.AbortReason.Should().BeNull();
    }

    [Test]
    public void Abort_WhenAlreadyDone_FailsWithInvalidState()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Done);

        Result result = session.Abort("operator", returnItem: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.invalid_state");
    }

    [Test]
    public void Complete_FromSettling_RecordsReceiptAndReleasesItem()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Settling);
        var receipt = SessionMother.Receipt();

        Result result = session.Complete(receipt);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Done);
        session.Receipt.Should().Be(receipt);
        session.ItemHeld.Should().BeFalse();
        session.IsTerminal.Should().BeTrue();
    }

    [Test]
    public void CompleteReturn_FromReturningItem_FinishesAndReleasesItem()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.ReturningItem);

        Result result = session.CompleteReturn();

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be(SessionStates.Done);
        session.ItemHeld.Should().BeFalse();
    }

    [Test]
    public void TryRecordIdempotencyKey_NewKey_ReturnsTrueAndLedgersIt()
    {
        TransactionSession session = SessionDriver.Begin();

        bool recorded = session.TryRecordIdempotencyKey("key-1", "tray/open", out string? existing);

        recorded.Should().BeTrue();
        existing.Should().BeNull();
        session.IdempotencyKeys.Should().Contain(new KeyValuePair<string, string>("key-1", "tray/open"));
    }

    [Test]
    public void TryRecordIdempotencyKey_ReplayedKey_ReturnsFalseWithOriginalOperation()
    {
        TransactionSession session = SessionDriver.Begin();
        session.TryRecordIdempotencyKey("key-1", "tray/open", out _);

        bool recorded = session.TryRecordIdempotencyKey("key-1", "tray/close", out string? existing);

        recorded.Should().BeFalse();
        existing.Should().Be("tray/open");
        session.IdempotencyKeys["key-1"].Should().Be("tray/open");
    }

    [Test]
    public void TryRecordIdempotencyKey_DistinctKeys_AreLedgeredIndependently()
    {
        TransactionSession session = SessionDriver.Begin();
        session.TryRecordIdempotencyKey("key-1", "tray/open", out _);

        bool recorded = session.TryRecordIdempotencyKey("key-2", "tray/close", out string? existing);

        recorded.Should().BeTrue();
        existing.Should().BeNull();
        session.IdempotencyKeys.Should().HaveCount(2);
    }

    [Test]
    public void AssignBagNumber_BlankValue_Throws()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Settling);

        session.Invoking(s => s.AssignBagNumber(" ")).Should().Throw<ArgumentException>();
    }

    [Test]
    public void AttachFolder_StoresTheTransactionFolder()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        session.AttachFolder(@"C:\data\transactions\02-07-2026\14-00-00");

        session.TransactionFolder.Should().Be(@"C:\data\transactions\02-07-2026\14-00-00");
    }

    public sealed record SessionCommand(
        string Name,
        string[] LegalFromStates,
        string TargetState,
        Func<TransactionSession, Result> Execute)
    {
        public override string ToString() => Name;
    }
}
