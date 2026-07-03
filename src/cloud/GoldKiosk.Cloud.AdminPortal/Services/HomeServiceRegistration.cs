namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>DI registration for Home dashboard services + the shared user/tenant context.</summary>
public static class HomeServiceRegistration
{
    /// <summary>Add home.</summary>
    public static IServiceCollection AddHome(this IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
