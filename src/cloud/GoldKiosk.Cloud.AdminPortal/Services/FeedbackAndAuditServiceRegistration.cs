
// ReSharper disable once CheckNamespace
namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// DI registrations for the Feedback + Audit module. Wire this from
/// <c>Program.cs</c> via <c>builder.Services.AddFeedbackAndAudit();</c>
/// to keep Program.cs free of per-module noise.
/// </summary>
public static class FeedbackAndAuditServiceRegistration
{
    /// <summary>Add feedback and audit.</summary>
    public static IServiceCollection AddFeedbackAndAudit(this IServiceCollection s)
    {
        s.AddScoped<IFeedbackService, FeedbackService>();
        s.AddScoped<IAuditLogService, AuditLogService>();
        return s;
    }
}
