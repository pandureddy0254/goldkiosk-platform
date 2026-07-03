namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>DI registration for voucher-management services.</summary>
public static class VoucherManagementServiceRegistration
{
    /// <summary>Add voucher management.</summary>
    public static IServiceCollection AddVoucherManagement(this IServiceCollection services)
    {
        services.AddScoped<IVoucherService, VoucherService>();
        return services;
    }
}
