using GoldKiosk.Cloud.AdminPortal.Models.Identity;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Backs the Roles &amp; permissions editor at <c>RolesPermissionsController</c>.
/// Distinct from <see cref="IRoleManagementService"/> (the role-CRUD service consumed
/// by the legacy Role master screen) and from <see cref="IAccessControlService"/>
/// (the legacy controller × action matrix).
/// </summary>
public interface IRolesPermissionsService
{
    /// <summary>
    /// Builds the index view-model: KPI strip, role cards, and the permission-editor
    /// content for <paramref name="selectedRoleId"/> (defaults to the Owner role when
    /// null or not found).
    /// </summary>
    Task<RolesIndexViewModel> GetIndexAsync(Guid? selectedRoleId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom (non-system) role with the given permission grants.
    /// Refuses codes that collide with system codes (owner / manager / operator /
    /// analyst / support / auditor).
    /// </summary>
    /// <returns>The new role's id, or <c>OperationResult.Fail</c> packed into the exception path.</returns>
    Task<OperationResult<Guid>> CreateCustomRoleAsync(
        string code,
        string name,
        string? description,
        IList<string> permissionCodes,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Replaces the role's permission grants with <paramref name="permissionCodes"/>.
    /// Diffs against the current set and inserts/deletes the minimum number of
    /// <c>identity.role_permissions</c> rows. Refuses system roles.
    /// </summary>
    Task<OperationResult> UpdatePermissionsAsync(
        Guid roleId,
        IList<string> permissionCodes,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a custom role (<c>deleted_at = now()</c>). Refuses system roles.
    /// </summary>
    Task<OperationResult> DeleteCustomRoleAsync(
        Guid roleId,
        Guid actorUserId,
        CancellationToken ct = default);
}

/// <summary>
/// Generic result wrapper. Mirrors the shape of <c>OperationResult</c> in
/// <c>Services/Common/PagedResult.cs</c> but carries a typed payload on success.
/// </summary>
public sealed record OperationResult<T>(bool Success, T? Value, IReadOnlyList<string> Errors)
{
    /// <summary>Join.</summary>
    public string ErrorSummary => Errors.Count == 0 ? string.Empty : string.Join("; ", Errors);
    /// <summary>Successful outcome carrying <paramref name="value"/>.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
        Justification = "Idiomatic factory on a generic record; call sites always know T.")]
    public static OperationResult<T> Ok(T value) => new(true, value, Array.Empty<string>());

    /// <summary>Failed outcome carrying <paramref name="errors"/>.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
        Justification = "Idiomatic factory on a generic record; call sites always know T.")]
    public static OperationResult<T> Fail(params string[] errors) => new(false, default, errors);
}
