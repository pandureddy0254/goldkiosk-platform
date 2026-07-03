
// ReSharper disable once CheckNamespace
namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// DI registrations for the Customer Management module. Wire this from
/// <c>Program.cs</c> via <c>builder.Services.AddCustomerManagement();</c>
/// to keep Program.cs free of per-module noise.
/// </summary>
public static class CustomerManagementServiceRegistration
{
    /// <summary>Add customer management.</summary>
    public static IServiceCollection AddCustomerManagement(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ITransactionReadService, TransactionReadService>();
        return services;
    }
}
