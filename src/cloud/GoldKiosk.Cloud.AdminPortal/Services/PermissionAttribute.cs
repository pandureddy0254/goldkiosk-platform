using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Authorise an action by permission code (e.g. <c>[Permission("kiosks:write")]</c>).
/// Codes come from <c>identity.permissions</c>; assignments live in
/// <c>identity.role_permissions</c>. The Owner role holds all 38 codes by
/// default.
///
/// Behaviour:
///   * Anonymous → redirect to login (delegated to the cookie scheme).
///   * Authenticated but missing the code → returns 403 (renders /Account/Denied
///     via the cookie scheme's AccessDeniedPath).
///   * Multiple codes on the same action → require ALL of them
///     (intersect semantics). For an OR, stack the attribute twice — first
///     check that returns Forbid short-circuits the pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _required;

    /// <summary>Initializes a new instance of the <see cref="PermissionAttribute"/> class.</summary>
    public PermissionAttribute(params string[] required)
    {
        _required = required ?? Array.Empty<string>();
    }

    /// <summary>On authorization.</summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var svc = context.HttpContext.RequestServices.GetRequiredService<IUserPermissionService>();
        var grants = await svc.GetGrantsAsync(context.HttpContext.RequestAborted);

        foreach (var code in _required)
        {
            if (!grants.Contains(code))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
