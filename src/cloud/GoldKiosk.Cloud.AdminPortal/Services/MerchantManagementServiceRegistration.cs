namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>DI registration for merchant-management services.</summary>
public static class MerchantManagementServiceRegistration
{
    /// <summary>Add merchant management.</summary>
    public static IServiceCollection AddMerchantManagement(this IServiceCollection services)
    {
        services.AddScoped<IMerchantService, MerchantService>();
        services.AddScoped<IFranchiseWalletService, FranchiseWalletService>();
        return services;
    }
}
