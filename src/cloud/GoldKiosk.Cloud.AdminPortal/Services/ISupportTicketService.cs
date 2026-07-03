using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i support ticket service.</summary>
/// <summary>I support ticket service.</summary>
public interface ISupportTicketService
{
    /// <summary>List.</summary>
    Task<SupportTicketMasterList> ListAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? categoryCode,
        string? subCategoryCode,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Get categories.</summary>
    Task<List<SelectListItem>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns sub-categories for the given parent category code, shaped as
    /// <c>[ { code, text }, ... ]</c> for the cascading-dropdown AJAX call.
    /// </summary>
    Task<IReadOnlyList<SubCategoryDto>> GetSubcategoriesAsync(string parentCategoryCode, CancellationToken ct = default);

    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(SupportTicketViewModel vm, string? documentsUri, CancellationToken ct = default);
}

/// <summary>Sub category DTO.</summary>
public sealed record SubCategoryDto(string Code, string Text);
