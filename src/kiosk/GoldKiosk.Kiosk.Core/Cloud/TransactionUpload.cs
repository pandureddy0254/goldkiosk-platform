namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// One completed transaction handed to <see cref="ICloudGateway.UploadTransactionAsync"/>
/// by the edge outbox (ADR 0002). The gateway maps <paramref name="JournalJson"/> to the
/// cloud wire record and forwards the images; the raw journal keeps the port free of wire
/// types.
/// </summary>
/// <param name="SessionId">The edge session id — the durable idempotency anchor.</param>
/// <param name="TransactionFolderPath">The transaction folder the record was read from.</param>
/// <param name="JournalJson">The raw <c>journal.json</c> contents for the completed session.</param>
/// <param name="ImagePaths">
/// Absolute paths of the non-PII item images to forward; PII/biometric images
/// (selfie, signature, ID) are excluded by the outbox and never enumerated here.
/// </param>
public sealed record TransactionUpload(
    string SessionId,
    string TransactionFolderPath,
    string JournalJson,
    IReadOnlyList<string> ImagePaths);
