using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>
/// Maps onto <c>kiosk.kiosks</c>. A physical GoldKiosk terminal at a store.
/// </summary>
public sealed class Kiosk : ITenantScoped, IAuditableEntity
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid StoreId { get; set; }
    /// <summary>Gets or sets the location id.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;            // citext
    /// <summary>Gets or sets the friendly name.</summary>
    public string FriendlyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the hardware model.</summary>
    public string HardwareModel { get; set; } = "GK-CUBE-V3";
    /// <summary>Gets or sets the device id.</summary>
    public string? DeviceId { get; set; }
    /// <summary>Gets or sets the msix channel.</summary>
    public string MsixChannel { get; set; } = "ring1";
    /// <summary>Gets or sets the app version.</summary>
    public string? AppVersion { get; set; }
    /// <summary>Gets or sets the os version.</summary>
    public string? OsVersion { get; set; }
    /// <summary>Gets or sets the cert thumbprint.</summary>
    public string? CertThumbprint { get; set; }
    /// <summary>Gets or sets the cert issued at.</summary>
    public DateTimeOffset? CertIssuedAt { get; set; }
    /// <summary>Gets or sets the cert expires at.</summary>
    public DateTimeOffset? CertExpiresAt { get; set; }
    /// <summary>Gets or sets the PIN hash.</summary>
    public string? PinHash { get; set; }
    /// <summary>Gets or sets the network speed.</summary>
    public string? NetworkSpeed { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "onboarding";           // text + check
    /// <summary>Gets or sets a value indicating whether is maintenance.</summary>
    public bool IsMaintenance { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the last ping at.</summary>
    public DateTimeOffset? LastPingAt { get; set; }
    /// <summary>Gets or sets the onboarded at.</summary>
    public DateTimeOffset? OnboardedAt { get; set; }
    /// <summary>Gets or sets the decommissioned at.</summary>
    public DateTimeOffset? DecommissionedAt { get; set; }

    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the updated at.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>Gets or sets the created by user id.</summary>
    public Guid? CreatedByUserId { get; set; }
    /// <summary>Gets or sets the updated by user id.</summary>
    public Guid? UpdatedByUserId { get; set; }
}
