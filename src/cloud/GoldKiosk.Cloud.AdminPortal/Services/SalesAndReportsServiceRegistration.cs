
// ReSharper disable once CheckNamespace
namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// DI registrations for the Sales + Reports module. Wire this from
/// <c>Program.cs</c> via <c>builder.Services.AddSalesAndReports();</c>
/// once Program.cs is updated.
/// </summary>
public static class SalesAndReportsServiceRegistration
{
    /// <summary>Add sales and reports.</summary>
    public static IServiceCollection AddSalesAndReports(this IServiceCollection s)
    {
        s.AddScoped<ISalesService, SalesService>();
        s.AddScoped<IReportsService, ReportsService>();
        return s;
    }
}
