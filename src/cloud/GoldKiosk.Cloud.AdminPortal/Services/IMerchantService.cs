using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i merchant service.</summary>
/// <summary>I merchant service.</summary>
public interface IMerchantService
{
    /// <summary>List.</summary>
    Task<MerchantList> ListAsync(string? search, string? kycStatus, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Get by id.</summary>
    Task<MerchantViewModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(MerchantViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(MerchantViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
