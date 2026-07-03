namespace GoldKiosk.Cloud.AdminPortal.Services.Invitations;

/// <summary>I user invitation service.</summary>
public interface IUserInvitationService
{
    /// <summary>Create a new invite, hash the token, persist, return the cleartext
    /// token so the controller can build the invite URL. Caller is responsible
    /// for sending the email.</summary>
    Task<CreateInviteResult> CreateAsync(
        Guid tenantId,
        Guid invitedByUserId,
        string inviteeEmail,
        string firstName,
        string lastName,
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>List pending.</summary>
    Task<List<PendingInviteRow>> ListPendingAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Look up an invitation by its cleartext token. Returns null if the
    /// token doesn't match a live (unaccepted, unrevoked, unexpired) invitation.</summary>
    Task<InvitationDetails?> ResolveAsync(string token, CancellationToken ct = default);

    /// <summary>Mark an invitation accepted by the given user id (called from the
    /// accept-invite handler after the user is created). Throws on race.</summary>
    Task MarkAcceptedAsync(Guid invitationId, Guid acceptedUserId, CancellationToken ct = default);

    /// <summary>Revoke.</summary>
    Task RevokeAsync(Guid invitationId, string reason, CancellationToken ct = default);

    /// <summary>Regenerate token.</summary>
    Task<string> RegenerateTokenAsync(Guid invitationId, CancellationToken ct = default);
}

/// <summary>Create invite result.</summary>
public sealed record CreateInviteResult(Guid InvitationId, string Token, DateTimeOffset ExpiresAt);

/// <summary>Invitation details.</summary>
public sealed class InvitationDetails
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the invitee email.</summary>
    public string InviteeEmail { get; set; } = "";
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = "";
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = "";
    /// <summary>Gets or sets the role id.</summary>
    public Guid RoleId { get; set; }
    /// <summary>Gets or sets the role code.</summary>
    public string RoleCode { get; set; } = "";
    /// <summary>Gets or sets the role name.</summary>
    public string RoleName { get; set; } = "";
    /// <summary>Gets or sets the tenant legal name.</summary>
    public string TenantLegalName { get; set; } = "";
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Pending invite row.</summary>
public sealed class PendingInviteRow
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the invitee email.</summary>
    public string InviteeEmail { get; set; } = "";
    /// <summary>Gets or sets the full name.</summary>
    public string FullName { get; set; } = "";
    /// <summary>Gets or sets the role name.</summary>
    public string RoleName { get; set; } = "";
    /// <summary>Gets or sets the invited at.</summary>
    public DateTimeOffset InvitedAt { get; set; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Gets or sets the resent count.</summary>
    public int ResentCount { get; set; }
}
