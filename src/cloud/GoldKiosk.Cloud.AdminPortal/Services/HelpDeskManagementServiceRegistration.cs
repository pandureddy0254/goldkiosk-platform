namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>DI registration for help-desk services (support tickets + SOS).</summary>
public static class HelpDeskManagementServiceRegistration
{
    /// <summary>Add help desk management.</summary>
    public static IServiceCollection AddHelpDeskManagement(this IServiceCollection services)
    {
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ISosService, SosService>();
        return services;
    }
}
