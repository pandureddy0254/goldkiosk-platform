using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Infrastructure;

/// <summary>
/// Global action filter that resolves the signed-in user's preferred theme from
/// crm.user_preferences and stores it in HttpContext.Items["Theme"] before the
/// action executes.
/// <para>
/// _Layout.cshtml reads Context.Items["Theme"] to write the data-theme attribute
/// server-side, eliminating the FOUC that occurred when only localStorage was used.
/// </para>
/// <para>
/// Fall-back order:
///   1. user_preferences row for this user  →  row.Theme
///   2. No row / RLS denied / transient error  →  "light"
/// </para>
/// </summary>
public sealed class ThemeFilter : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var ctx = context.HttpContext;
        var currentUser = ctx.RequestServices.GetRequiredService<ICurrentUserService>();

        if (currentUser.UserId is Guid uid && !ctx.Items.ContainsKey("Theme"))
        {
            try
            {
                var db = ctx.RequestServices.GetRequiredService<CrmDbContext>();
                var theme = await db.UserPreferences
                    .AsNoTracking()
                    .Where(p => p.UserId == uid)
                    .Select(p => p.Theme)
                    .FirstOrDefaultAsync(ctx.RequestAborted);

                if (theme is "light" or "dark")
                {
                    ctx.Items["Theme"] = theme;
                }
            }
            catch (Exception)
            {
                // Transient DB error or RLS denial — fall through to default.
                // A theme preference failure must never break page rendering.
            }
        }

        // Guarantee the key is always present so _Layout.cshtml never throws.
        if (!ctx.Items.ContainsKey("Theme"))
        {
            ctx.Items["Theme"] = "light";
        }

        await next();
    }
}
