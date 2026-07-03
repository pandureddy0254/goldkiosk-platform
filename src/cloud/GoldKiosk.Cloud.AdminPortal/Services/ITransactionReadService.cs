using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i transaction read service.</summary>
/// <summary>I transaction read service.</summary>
public interface ITransactionReadService
{
    /// <summary>List for customer.</summary>
    Task<TransactionHistoryList> ListForCustomerAsync(
        string customerCode,
        string? search,
        string? status,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Reverse.</summary>
    Task<OperationResult> ReverseAsync(Guid transactionId, CancellationToken ct = default);
}
