using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i customer service.</summary>
/// <summary>I customer service.</summary>
public interface ICustomerService
{
    /// <summary>List.</summary>
    Task<CustomerMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Get by code.</summary>
    Task<CustomerMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(CustomerMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(CustomerMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default);
}
