using System.Reflection;
using GoldKiosk.Kiosk.Devices.Exceptions;

namespace GoldKiosk.Kiosk.Devices.Real.Vendor;

/// <summary>
/// Reflection loading for vendor SDK assemblies (Advantech BDaq, ARCA Envoy, Acuant, Gemalto,
/// FlexCode, Camera_NET) and COM ProgIDs (b-PAC, Innov-X). These SDKs are proprietary
/// binaries provisioned onto kiosk machines (see <c>VENDOR-SDKS.md</c>) and never referenced
/// at compile time; when one is absent the owning driver reports <c>Faulted</c> health with a
/// <c>vendor_sdk_missing:*</c> detail instead of failing composition.
/// </summary>
internal static class VendorSdkLoader
{
    /// <summary>Builds the canonical missing-SDK health detail.</summary>
    /// <param name="sdkName">Short SDK identifier (e.g. <c>Automation.BDaq</c>).</param>
    internal static string MissingDetail(string sdkName) => $"vendor_sdk_missing:{sdkName}";

    /// <summary>
    /// Loads a vendor assembly from the given path (relative paths resolve against
    /// <see cref="AppContext.BaseDirectory"/>), or throws
    /// <see cref="DeviceConnectFailedException"/> carrying the <c>vendor_sdk_missing</c> detail.
    /// </summary>
    /// <param name="assemblyPath">Vendor assembly path from configuration.</param>
    /// <param name="sdkName">Short SDK identifier used in the failure detail.</param>
    internal static Assembly LoadAssembly(string assemblyPath, string sdkName)
    {
        string fullPath = Path.IsPathRooted(assemblyPath)
            ? assemblyPath
            : Path.Combine(AppContext.BaseDirectory, assemblyPath);

        if (!File.Exists(fullPath))
        {
            throw new DeviceConnectFailedException(MissingDetail(sdkName));
        }

        try
        {
            return Assembly.LoadFrom(fullPath);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
        {
            throw new DeviceConnectFailedException(MissingDetail(sdkName), ex);
        }
    }

    /// <summary>Loads every additional assembly matching a wildcard next to an already-loaded vendor assembly (e.g. the IKVM runtime beside LibEnvoyAPI).</summary>
    /// <param name="anchor">The vendor assembly whose directory is scanned.</param>
    /// <param name="searchPattern">File pattern, e.g. <c>IKVM.*.dll</c>.</param>
    /// <returns>The loaded companions (load failures are skipped — the type lookup decides fatality).</returns>
    internal static IReadOnlyList<Assembly> LoadCompanions(Assembly anchor, string searchPattern)
    {
        string? directory = Path.GetDirectoryName(anchor.Location);
        if (directory is null || !Directory.Exists(directory))
        {
            return [];
        }

        List<Assembly> loaded = [];
        foreach (string file in Directory.EnumerateFiles(directory, searchPattern))
        {
            try
            {
                loaded.Add(Assembly.LoadFrom(file));
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
            {
                // Companion probing is opportunistic; missing types fault later with a
                // precise detail from GetRequiredType/FindType.
            }
        }

        return loaded;
    }

    /// <summary>Resolves a required type from a loaded vendor assembly or throws the connect-failure detail.</summary>
    /// <param name="assembly">The vendor assembly.</param>
    /// <param name="typeName">Full type name (first match wins across candidates).</param>
    /// <param name="sdkName">Short SDK identifier used in the failure detail.</param>
    internal static Type GetRequiredType(Assembly assembly, string typeName, string sdkName) =>
        assembly.GetType(typeName, throwOnError: false)
        ?? throw new DeviceConnectFailedException($"{MissingDetail(sdkName)}:{typeName}");

    /// <summary>Finds a type by full name across a set of assemblies (used for the IKVM-compiled java.* types).</summary>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <param name="typeName">Full type name.</param>
    /// <param name="sdkName">Short SDK identifier used in the failure detail.</param>
    internal static Type FindType(IEnumerable<Assembly> assemblies, string typeName, string sdkName)
    {
        foreach (Assembly assembly in assemblies)
        {
            if (assembly.GetType(typeName, throwOnError: false) is { } type)
            {
                return type;
            }
        }

        throw new DeviceConnectFailedException($"{MissingDetail(sdkName)}:{typeName}");
    }

    /// <summary>Resolves a registered COM type by ProgID or throws the connect-failure detail.</summary>
    /// <param name="progId">The COM ProgID (e.g. <c>bpac.Document</c>).</param>
    internal static Type GetComType(string progId) =>
        Type.GetTypeFromProgID(progId, throwOnError: false)
        ?? throw new DeviceConnectFailedException(MissingDetail(progId));

    /// <summary>Late-bound instance property get.</summary>
    internal static object? GetProperty(object target, string name) =>
        target.GetType().InvokeMember(
            name, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, target, null,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Late-bound instance property set.</summary>
    internal static void SetProperty(object target, string name, object? value) =>
        target.GetType().InvokeMember(
            name, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance, null, target, [value],
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Late-bound instance method invoke.</summary>
    internal static object? Invoke(object target, string name, params object?[] args) =>
        target.GetType().InvokeMember(
            name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, target, args,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Late-bound static method invoke on a resolved type.</summary>
    internal static object? InvokeStatic(Type type, string name, params object?[] args) =>
        type.InvokeMember(
            name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, args,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Reads a public static field (IKVM-compiled java enums expose values as static fields).</summary>
    internal static object? GetStaticField(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
}
