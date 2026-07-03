using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoldKiosk.Cloud.CRMPortal.Infrastructure;

/// <summary>
/// Apply to actions/controllers that require manager-level access (sales_manager
/// or superadmin). Anything else (sales_rep) gets a 403. Mirrors the DB-side
/// crm.is_sales_manager() gate so the UI fails fast before the RPC would reject.
/// </summary>
/// <remarks>
/// Ported faithfully from the legacy CRM as a filter attribute; a follow-up ticket
/// will rewrite it as a policy-based authorization requirement.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireManagerAttribute : Attribute, IAuthorizationFilter
{
    /// <inheritdoc/>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (!currentUser.IsManager)
        {
            context.Result = new ForbidResult();
        }
    }
}
