namespace GoldKiosk.Contracts.V1.Identity;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/identity/signature</c> — the on-screen
/// signature clubbed with the terms version it signs.
/// </summary>
/// <param name="SignaturePngBase64">The signature image as base64-encoded PNG.</param>
/// <param name="SignedTermsVersion">The terms version the signature applies to, e.g. <c>2026-06-01.v3</c>.</param>
public sealed record IdentitySignatureRequest(string SignaturePngBase64, string SignedTermsVersion);
