using GoldKiosk.Cloud.AdminPortal.Models.Tenancy;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Loads + persists the tenant-settings page payload (General · Branding ·
/// Features · Activation · Billing · Compliance). All reads and writes are
/// scoped to the signed-in user's <c>tenant_id</c>; cleartext activation keys
/// are never returned by any method on this surface.
/// </summary>
public interface ITenantSettingsService
{
    /// <summary>Get.</summary>
    Task<TenantSettingsViewModel> GetAsync(CancellationToken ct = default);

    /// <summary>Save general.</summary>
    Task SaveGeneralAsync(TenantGeneralForm form, Guid actorUserId, CancellationToken ct = default);
    /// <summary>Save branding.</summary>
    Task SaveBrandingAsync(TenantBrandingForm form, Guid actorUserId, CancellationToken ct = default);
    /// <summary>Save features.</summary>
    Task SaveFeaturesAsync(IDictionary<string, bool> featureFlags, Guid actorUserId, CancellationToken ct = default);
    /// <summary>Save retention.</summary>
    Task SaveRetentionAsync(TenantRetentionForm form, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Immutable history of activation keys for the current tenant — no cleartext.</summary>
    Task<IReadOnlyList<ActivationKeyHistoryRow>> GetActivationKeyHistoryAsync(CancellationToken ct = default);
}
