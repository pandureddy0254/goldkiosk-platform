namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>DI registration for the Operations module (deployment / maintenance /
/// technicians / collection / location config / kiosk security tokens / system health).</summary>
public static class OperationsServiceRegistration
{
    /// <summary>Add operations.</summary>
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        services.AddScoped<IDeploymentTicketService, DeploymentTicketService>();
        services.AddScoped<IMaintenanceTicketService, MaintenanceTicketService>();
        services.AddScoped<ITechnicianService, TechnicianService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ILocationConfigurationService, LocationConfigurationService>();
        services.AddScoped<IKioskSecurityTokenService, KioskSecurityTokenService>();
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        return services;
    }
}
