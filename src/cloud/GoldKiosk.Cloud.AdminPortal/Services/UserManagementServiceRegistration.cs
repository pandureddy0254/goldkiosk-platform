namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// DI registration for the User Management module (roles, users, access control).
/// Call from <c>Program.cs</c>: <c>builder.Services.AddUserManagementServices();</c>.
/// </summary>
public static class UserManagementServiceRegistration
{
    /// <summary>Add user management services.</summary>
    public static IServiceCollection AddUserManagementServices(this IServiceCollection s)
    {
        s.AddScoped<IRoleManagementService, RoleManagementService>();
        s.AddScoped<IUserAdministrationService, UserAdministrationService>();
        s.AddScoped<IAccessControlService, AccessControlService>();
        s.AddScoped<IRolesPermissionsService, RolesPermissionsService>();
        return s;
    }
}
