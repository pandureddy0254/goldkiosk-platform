using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i voucher service.</summary>
/// <summary>I voucher service.</summary>
public interface IVoucherService
{
    // ─── Vouchers ───────────────────────────────────────────────────────────
    /// <summary>List vouchers.</summary>
    Task<VoucherList> ListVouchersAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Get voucher.</summary>
    Task<VoucherViewModel?> GetVoucherAsync(Guid id, CancellationToken ct = default);
    /// <summary>Add voucher.</summary>
    Task<OperationResult> AddVoucherAsync(VoucherViewModel vm, CancellationToken ct = default);
    /// <summary>Edit voucher.</summary>
    Task<OperationResult> EditVoucherAsync(VoucherViewModel vm, CancellationToken ct = default);
    /// <summary>Delete voucher.</summary>
    Task<OperationResult> DeleteVoucherAsync(Guid id, CancellationToken ct = default);

    // ─── Redemption policies ───────────────────────────────────────────────
    /// <summary>List policies.</summary>
    Task<RedemptionPolicyList> ListPoliciesAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add policy.</summary>
    Task<OperationResult> AddPolicyAsync(RedemptionPolicyViewModel vm, CancellationToken ct = default);
    /// <summary>Edit policy.</summary>
    Task<OperationResult> EditPolicyAsync(RedemptionPolicyViewModel vm, CancellationToken ct = default);
    /// <summary>Delete policy.</summary>
    Task<OperationResult> DeletePolicyAsync(Guid id, CancellationToken ct = default);

    // ─── Voucher redemptions (read-only) ───────────────────────────────────
    /// <summary>List redemptions.</summary>
    Task<VoucherRedemptionList> ListRedemptionsAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default);
}
