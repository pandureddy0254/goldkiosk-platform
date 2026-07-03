using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoldKiosk.Cloud.CRMPortal.Infrastructure;

/// <summary>
/// Apply to actions/controllers reserved for the superadmin (e.g. member
/// management). Anyone else — including sales_manager — gets a 403. Mirrors the
/// DB-side crm.is_superadmin() gate on crm.create_member.
/// </summary>
/// <remarks>
/// Ported faithfully from the legacy CRM as a filter attribute; a follow-up ticket
/// will rewrite it as a policy-based authorization requirement.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireSuperadminAttribute : Attribute, IAuthorizationFilter
{
    /// <inheritdoc/>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (!currentUser.IsSuperadmin)
        {
            context.Result = new ForbidResult();
        }
    }
}
