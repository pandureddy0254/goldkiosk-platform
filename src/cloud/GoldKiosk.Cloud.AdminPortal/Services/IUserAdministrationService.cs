using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i user administration service.</summary>
/// <summary>I user administration service.</summary>
public interface IUserAdministrationService
{
    /// <summary>List.</summary>
    Task<UserRoleMapVM> ListAsync(string? search, string? roleCode, string? status, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Get by id.</summary>
    Task<UserRoleMapVM?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(UserRoleMapVM vm, CancellationToken ct = default);

    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(UserRoleMapVM vm, CancellationToken ct = default);

    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Assign role.</summary>
    Task<OperationResult> AssignRoleAsync(Guid userId, string roleCode, CancellationToken ct = default);

    /// <summary>Remove role.</summary>
    Task<OperationResult> RemoveRoleAsync(Guid userId, string roleCode, CancellationToken ct = default);
}
