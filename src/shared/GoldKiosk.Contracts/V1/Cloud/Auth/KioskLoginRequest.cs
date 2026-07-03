namespace GoldKiosk.Contracts.V1.Cloud.Auth;

/// <summary>
/// Request body for <c>POST /api/v1/auth/kiosk-login</c> — machine sign-in with the
/// provisioning code + PIN pair (evolves toward per-device certificate identity).
/// </summary>
/// <param name="Code">The kiosk's fleet code assigned at provisioning, e.g. <c>GK-MUM-004</c>.</param>
/// <param name="Pin">The kiosk PIN set at provisioning; verified against the stored Argon2id hash.</param>
public sealed record KioskLoginRequest(string Code, string Pin);
