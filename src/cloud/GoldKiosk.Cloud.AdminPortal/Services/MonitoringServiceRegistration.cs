
// ReSharper disable once CheckNamespace
namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// DI registrations for the Monitoring module. Wire from <c>Program.cs</c> via
/// <c>builder.Services.AddMonitoring();</c>. Keeps Program.cs free of
/// per-module noise.
/// </summary>
public static class MonitoringServiceRegistration
{
    /// <summary>Add monitoring.</summary>
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        services.AddScoped<IExceptionMonitoringService, ExceptionMonitoringService>();
        services.AddScoped<IApiMonitoringService, ApiMonitoringService>();
        services.AddScoped<IUserActivityLogService, UserActivityLogService>();
        services.AddScoped<IKioskInventoryMonitoringService, KioskInventoryMonitoringService>();
        return services;
    }
}
