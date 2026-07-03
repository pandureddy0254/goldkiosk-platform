namespace GoldKiosk.Kiosk.UI.Resources;

/// <summary>
/// The complete en-US string catalogue. Every visible string on the kiosk resolves here;
/// additional locales mirror this key set.
/// </summary>
public static class StringsEnUs
{
    /// <summary>Gets the en-US key/value catalogue.</summary>
    public static IReadOnlyDictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Common
        ["common.continue"] = "Continue",
        ["common.close"] = "Close",
        ["common.back"] = "Back",
        ["common.ok"] = "OK",

        // Attract loop
        ["attract.we_buy"] = "We buy",
        ["attract.pawn"] = "Pawn",
        ["attract.word.gold"] = "Gold",
        ["attract.word.silver"] = "Silver",
        ["attract.touch_to_begin"] = "TOUCH TO BEGIN",
        ["attract.rates.gold"] = "GOLD / G",
        ["attract.rates.silver"] = "SILVER / G",

        // Welcome / service selection
        ["welcome.kicker"] = "SERVICE",
        ["welcome.title"] = "What would you like to do?",
        ["welcome.sell.title"] = "Sell",
        ["welcome.sell.subtitle"] = "Instant offer · paid today",
        ["welcome.pawn.title"] = "Pawn",
        ["welcome.pawn.subtitle"] = "Borrow against it · keep ownership",
        ["welcome.terms.label"] = "I agree to the terms & conditions",
        ["welcome.terms.link"] = "View terms",
        ["welcome.cancel"] = "Never mind",
        ["terms.sheet.title"] = "Terms & conditions",
        ["terms.sheet.body"] = "By continuing you confirm the item is yours to sell or pawn, "
            + "that you are of legal age, and that you accept the transaction terms displayed "
            + "for your region. The full terms travel with your receipt.",

        // Place item
        ["place.kicker"] = "PLACE ITEM",
        ["place.title.opening"] = "Opening the tray",
        ["place.title.open"] = "Place your item in the tray",
        ["place.title.closing"] = "Closing the tray",
        ["place.title.closed"] = "One moment",
        ["place.subtitle"] = "One item at a time — lay it flat in the lit tray below.",
        ["place.confirm"] = "I've placed my item",
        ["place.backout"] = "I changed my mind",

        // Analysis
        ["analysis.title"] = "Analysing your item",
        ["analysis.subtitle"] = "Our AI is verifying authenticity and checking the live market — one moment.",
        ["analysis.step.item_detected"] = "Item detected & classified",
        ["analysis.step.authenticating"] = "Verifying authenticity",
        ["analysis.step.pricing"] = "Pricing against live market",

        // Offer
        ["offer.chip.ai_verified"] = "AI · VERIFIED",
        ["offer.kicker"] = "YOUR OFFER",
        ["offer.chip.verified"] = "Verified",
        ["offer.chip.live_price"] = "Live price",
        ["offer.chip.expired"] = "Offer expired",
        ["offer.explain.link"] = "How was this calculated?",
        ["offer.accept"] = "Accept offer",
        ["offer.return"] = "Return item",
        ["offer.sheet.title"] = "How we calculated this",
        ["offer.sheet.loading"] = "Asking our AI…",
        ["offer.pawn.title"] = "Pawn terms",
        ["offer.pawn.monthly_fee"] = "Monthly fee",
        ["offer.pawn.apr"] = "APR",
        ["offer.pawn.total_repayment"] = "Total to redeem",
        ["offer.pawn.due_date"] = "Redeem by",

        // Identity
        ["identity.title"] = "Verify it's you",
        ["identity.subtitle"] = "A quick, secure check — scan your ID and glance at the camera. Seconds, fully encrypted.",
        ["identity.step.id_scan"] = "ID scanned",
        ["identity.step.face_match"] = "Face match",
        ["identity.step.fingerprint"] = "Fingerprint",
        ["identity.step.signature"] = "Signature",
        ["signature.kicker"] = "SIGNATURE",
        ["signature.title"] = "Sign to accept",
        ["signature.subtitle"] = "Use your finger — sign inside the frame.",
        ["signature.clear"] = "Clear",
        ["signature.submit"] = "Sign & continue",

        // Contact & receipt
        ["contact.kicker"] = "RECEIPT",
        ["contact.title"] = "Where should we send your receipt?",
        ["contact.subtitle"] = "A QR receipt always shows on screen. Add email or SMS if you'd like a copy.",
        ["contact.email.label"] = "Email",
        ["contact.phone.label"] = "Phone",
        ["contact.channel.qr"] = "QR",
        ["contact.channel.email"] = "Email",
        ["contact.channel.sms"] = "SMS",
        ["contact.skip"] = "QR only",

        // Payout
        ["payout.kicker"] = "PAYOUT",
        ["payout.title"] = "How would you like your cash?",
        ["payout.cash.title"] = "Instant cash",
        ["payout.cash.subtitle"] = "Dispensed now",
        ["payout.cash.unavailable"] = "Unavailable for this amount",
        ["payout.bank_transfer.title"] = "Bank transfer",
        ["payout.bank_transfer.subtitle"] = "1–2 hours · free",
        ["payout.debit_card.title"] = "Debit card",
        ["payout.debit_card.subtitle"] = "Instant",
        ["payout.bank.account_holder"] = "Account holder",
        ["payout.bank.routing_number"] = "Routing number",
        ["payout.bank.account_number"] = "Account number",
        ["payout.bank.type.checking"] = "Checking",
        ["payout.bank.type.savings"] = "Savings",

        // Processing / settlement
        ["processing.kicker"] = "PROCESSING",
        ["processing.stage.bagging"] = "Securing your item",
        ["processing.stage.dispensing"] = "Dispensing your cash",
        ["processing.stage.transferring"] = "Transferring your funds",
        ["processing.stage.returning"] = "Returning your item",
        ["processing.subtitle"] = "Please stay at the kiosk — this takes a moment.",
        ["processing.bills"] = "{0} bills",

        // Done
        ["done.title"] = "All done",
        ["done.message.cash"] = "Your cash has been dispensed. Take your receipt below.",
        ["done.message.bank_transfer"] = "Your transfer is on its way. Take your receipt below.",
        ["done.message.debit_card"] = "Your card payout is complete. Take your receipt below.",
        ["done.message.generic"] = "Thanks for visiting. Take your receipt below.",
        ["done.message.returned"] = "Your item has been returned. Thanks for visiting.",
        ["done.message.aborted"] = "The session has ended. Please collect your item from the tray.",
        ["done.receipt.label"] = "RECEIPT · SCAN OR TAP",
        ["done.ended.title"] = "Session ended",

        // Timeout overlay
        ["timeout.title"] = "Are you still there?",
        ["timeout.subtitle"] = "The session ends in",
        ["timeout.resume"] = "Yes — I'm here",
        ["timeout.abort"] = "No — end session",

        // Live agent overlay
        ["agent.connecting.title"] = "Connecting you to a specialist",
        ["agent.connecting.subtitle"] = "A human review takes under a minute.",
        ["agent.joined.title"] = "A specialist is reviewing your item",
        ["agent.joined.subtitle"] = "Almost there — thank you for waiting.",
        ["agent.chip"] = "LIVE · SPECIALIST",

        // Device banner
        ["device.degraded"] = "Some services are limited on this kiosk right now.",
        ["device.faulted"] = "This kiosk needs attention — staff has been notified.",

        // Error overlay actions
        ["error.action.ok"] = "OK",
        ["error.action.retry"] = "Try again",
        ["error.action.return_item"] = "Return my item",
        ["error.action.end_session"] = "End session",
        ["error.action.choose_method"] = "Choose another way",
        ["error.action.back_to_start"] = "Back to start",

        // Generic errors
        ["error.generic.title"] = "Something went wrong",
        ["error.generic.message"] = "We couldn't complete that step. Please try again.",
        ["error.connectivity.title"] = "One moment",
        ["error.connectivity.message"] = "The kiosk is reconnecting. Please try again in a few seconds.",

        // Rejection reasons (RejectionReasonCodes)
        ["error.item.empty_tray.title"] = "We didn't find an item",
        ["error.item.empty_tray.message"] = "The tray closed empty. Place one item flat in the tray and try again.",
        ["error.item.multiple_items.title"] = "One item at a time",
        ["error.item.multiple_items.message"] = "Please place a single item in the tray — we'll return these to you now.",
        ["error.item.unidentified.title"] = "We couldn't identify this item",
        ["error.item.unidentified.message"] = "Our AI couldn't classify your item, so we're returning it to you.",
        ["error.item.unaccepted_type.title"] = "We can't accept this item",
        ["error.item.unaccepted_type.message"] = "This kiosk accepts jewellery only — your item is being returned.",
        ["error.item.underweight.title"] = "Item below minimum weight",
        ["error.item.underweight.message"] = "This item is lighter than our accepted minimum, so we're returning it.",
        ["error.item.gold_plated.title"] = "Plated item detected",
        ["error.item.gold_plated.message"] = "Our analysis shows plating rather than solid precious metal. Your item is being returned.",
        ["error.item.insufficient_purity.title"] = "Purity below minimum",
        ["error.item.insufficient_purity.message"] = "The measured purity is below what we can accept. Your item is being returned.",
        ["error.kyc.underage.title"] = "Age requirement not met",
        ["error.kyc.underage.message"] = "You must be of legal age to use this service. Your item is being returned.",
        ["error.kyc.id_expired.title"] = "ID expired",
        ["error.kyc.id_expired.message"] = "The document you scanned has expired. Please use a valid government-issued ID.",
        ["error.kyc.not_govt_id.title"] = "ID not accepted",
        ["error.kyc.not_govt_id.message"] = "Please scan a government-issued photo ID.",
        ["error.kyc.blacklisted.title"] = "We can't proceed",
        ["error.kyc.blacklisted.message"] = "We're unable to complete this transaction. Your item is being returned.",
        ["error.kyc.face_mismatch.title"] = "Face didn't match",
        ["error.kyc.face_mismatch.message"] = "We couldn't match your face to the ID photo. Look straight at the camera and try again.",
        ["error.payout.insufficient_cash.title"] = "Cash unavailable for this amount",
        ["error.payout.insufficient_cash.message"] = "This kiosk can't cover your offer in cash right now. Choose bank transfer or debit card, or take your item back.",

        // Problem types (ProblemTypes)
        ["error.session.not_found.title"] = "Session not found",
        ["error.session.not_found.message"] = "This session is no longer available. Let's start over.",
        ["error.session.invalid_state.title"] = "Let's get back in sync",
        ["error.session.invalid_state.message"] = "That action isn't available right now. One moment.",
        ["error.session.expired.title"] = "Session expired",
        ["error.session.expired.message"] = "Your session timed out. Let's start over.",
        ["error.tray.blocked.title"] = "The tray is blocked",
        ["error.tray.blocked.message"] = "Please clear anything blocking the tray and try again.",
        ["error.tray.hardware_fault.title"] = "Tray needs attention",
        ["error.tray.hardware_fault.message"] = "The tray reported a fault. Staff has been notified.",
        ["error.offer.expired.title"] = "Offer expired",
        ["error.offer.expired.message"] = "The market moved and this offer's lock ran out. We'll return your item, or start again for a fresh offer.",
        ["error.offer.already_actioned.title"] = "Already answered",
        ["error.offer.already_actioned.message"] = "This offer was already accepted or declined.",
        ["error.identity.step_failed.title"] = "Verification hiccup",
        ["error.identity.step_failed.message"] = "That step didn't complete. Let's try it again.",
        ["error.identity.retries_exhausted.title"] = "We couldn't verify you",
        ["error.identity.retries_exhausted.message"] = "Verification didn't succeed. Your item is being returned.",
        ["error.payout.invalid_bank_details.title"] = "Check your bank details",
        ["error.payout.invalid_bank_details.message"] = "Those bank details didn't validate. Please check them and try again.",
        ["error.settle.authorization_required.title"] = "Authorization needed",
        ["error.settle.authorization_required.message"] = "We need a network authorization to finish. One moment — or return your item.",
        ["error.settle.hardware_fault.title"] = "We hit a snag",
        ["error.settle.hardware_fault.message"] = "The machine reported a fault while finishing up. Staff has been notified.",
        ["error.device.unavailable.title"] = "Service unavailable",
        ["error.device.unavailable.message"] = "A required device is unavailable right now. Staff has been notified.",
        ["error.idempotency.key_conflict.title"] = "Please try again",
        ["error.idempotency.key_conflict.message"] = "That request couldn't be processed safely. Please try again.",

        // Identity retry hint
        ["error.identity.retries_left"] = "{0} attempts left",
    };
}
