namespace GoldKiosk.Contracts.V1.Identity;

/// <summary>
/// Progress through the hardware-driven identity sequence:
/// <c>id_scan</c> → <c>face_match</c> → <c>fingerprint</c> (config-gated) → <c>signature</c>.
/// </summary>
/// <param name="CurrentStep">The step currently in progress, e.g. <c>id_scan</c>.</param>
/// <param name="Steps">The per-step statuses for the checklist screen.</param>
public sealed record IdentityProgressDto(string CurrentStep, IReadOnlyList<IdentityStepDto> Steps);
