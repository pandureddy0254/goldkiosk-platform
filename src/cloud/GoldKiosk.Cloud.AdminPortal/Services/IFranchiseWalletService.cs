using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i franchise wallet service.</summary>
/// <summary>I franchise wallet service.</summary>
public interface IFranchiseWalletService
{
    /// <summary>List.</summary>
    Task<FranchiseWalletsList> ListAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Approve topup.</summary>
    Task<OperationResult> ApproveTopupAsync(Guid topupId, CancellationToken ct = default);
    /// <summary>Reject topup.</summary>
    Task<OperationResult> RejectTopupAsync(Guid topupId, string? reason, CancellationToken ct = default);
}
