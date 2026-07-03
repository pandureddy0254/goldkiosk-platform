using System.Reflection;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real cash dispenser driver: Fujitsu F53 behind the ARCA Envoy Java-RMI service, driven by
/// reflection over the IKVM-compiled <c>LibEnvoyAPI.dll</c> (plus its <c>IKVM.*.dll</c>
/// companions) — no compile-time vendor reference. Connect ports the legacy
/// <c>GCCashDispenser.InitDevice</c>: <c>java.rmi.Naming.lookup("//host/envoy/system")</c>,
/// find-or-register the F53, then look up the device path. Planning is the pure greedy
/// algorithm in <see cref="BillMixPlanner"/> against cassette presence from
/// <c>deviceStatus()</c>; dispensing ports <c>dispenseByPosition</c> with per-position
/// dispensed/rejected verification. Idempotency of dispense commands is enforced upstream.
/// SDK absent → <c>vendor_sdk_missing:LibEnvoyAPI</c>.
/// </summary>
public sealed class RealCashDispenser : RealDeviceBase, ICashDispenser
{
    private const string SdkName = "LibEnvoyAPI";
    private const string F53DeviceName = "FUJITSU_F53";

    private readonly ConnectionOptions _connection;
    private readonly CashDispenserOptions _options;
    private readonly Lock _cassetteLock = new();
    private readonly Dictionary<int, int> _cassetteIndexByDenomination = [];
    private IReadOnlyList<Assembly> _sdkAssemblies = [];
    private object? _device;
    private Type? _javaIntegerType;
    private Type? _hashMapType;
    private Type? _dispensePrmType;

    /// <summary>Initializes the driver with legacy-parity defaults (localhost Envoy).</summary>
    public RealCashDispenser()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (SDK path, Envoy host); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Dispenser and planner tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealCashDispenser(
        ConnectionOptions? connection,
        CashDispenserOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.CashDispenser, "Cash dispenser (ARCA Envoy F53)", isCritical: false, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions())
            .MergedWith(RealDeviceDefaults.For(DeviceKeys.CashDispenser));
        _options = options ?? new CashDispenserOptions();
    }

    /// <inheritdoc />
    public async Task<BillMixPlan> PlanAsync(long amountMinor, CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        Dictionary<int, int> availability = await Task.Run(ReadCassetteAvailability, cancellationToken)
            .ConfigureAwait(false);
        return BillMixPlanner.Plan(
            amountMinor,
            _options.MinorUnitsPerMajorUnit,
            availability,
            _options.MinimumBillsReserve);
    }

    /// <inheritdoc />
    public async Task<DispenseResult> DispenseAsync(BillMixPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        EnsureOperable();
        if (!plan.Feasible || plan.Bills.Count == 0)
        {
            return new DispenseResult(false, new Dictionary<int, int>(), "plan_infeasible");
        }

        SetHealth(DeviceState.Busy, "Dispensing cash.");
        try
        {
            DispenseResult result = await Task.Run(() => DispenseCore(plan), cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"dispense_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Assembly envoy = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, SdkName);
            List<Assembly> assemblies = [envoy, .. VendorSdkLoader.LoadCompanions(envoy, "IKVM.*.dll")];
            _sdkAssemblies = assemblies;

            Type naming = VendorSdkLoader.FindType(assemblies, "java.rmi.Naming", SdkName);
            _javaIntegerType = VendorSdkLoader.FindType(assemblies, "java.lang.Integer", SdkName);
            _hashMapType = VendorSdkLoader.FindType(assemblies, "java.util.HashMap", SdkName);
            _dispensePrmType = VendorSdkLoader.FindType(
                assemblies, "com.arca.envoy.api.iface.FujitsuDispenseByPositionPrm", SdkName);

            object envoySystem = VendorSdkLoader.InvokeStatic(
                    naming, "lookup", $"//{_connection.Host}/envoy/system")
                ?? throw new DeviceConnectFailedException("envoy_system_unavailable");

            string devicePath = FindOrRegisterF53(envoySystem)
                ?? throw new DeviceConnectFailedException("f53_not_found");
            _device = VendorSdkLoader.InvokeStatic(naming, "lookup", devicePath)
                ?? throw new DeviceConnectFailedException("f53_lookup_failed");

            // Legacy init parity (media mappings + mechanical reset with USD bill params) is
            // best-effort: the API surface varies across Envoy versions and the device
            // remains dispensable without it; failures surface later as status errors.
            TryLegacyInit(assemblies, _device);
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        _device = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_device is null)
        {
            return new DeviceProbeResult(false, 0, Health.Detail ?? "Dispenser is not connected.");
        }

        Dictionary<int, int> availability = await Task.Run(ReadCassetteAvailability, cancellationToken)
            .ConfigureAwait(false);
        return availability.Count > 0
            ? new DeviceProbeResult(true, 0, $"Cassettes online for {availability.Count} denominations.")
            : new DeviceProbeResult(false, 0, "f53_no_usable_cassettes");
    }

    private static string? FindOrRegisterF53(object envoySystem)
    {
        if (VendorSdkLoader.Invoke(envoySystem, "getRegisteredDeviceNames")
            is System.Collections.IEnumerable registeredNames)
        {
            foreach (object? name in registeredNames)
            {
                if (name is null)
                {
                    continue;
                }

                object? info = VendorSdkLoader.Invoke(envoySystem, "getRegisteredDeviceInformation", name);
                if (info is not null && IsF53(info))
                {
                    return VendorSdkLoader.Invoke(envoySystem, "getDevicePath", name) as string;
                }
            }
        }

        if (VendorSdkLoader.Invoke(envoySystem, "getAllKnownDeviceInformation")
            is System.Collections.IEnumerable knownDevices)
        {
            foreach (object? info in knownDevices)
            {
                if (info is null || !IsF53(info))
                {
                    continue;
                }

                if (VendorSdkLoader.Invoke(envoySystem, "register", F53DeviceName, info) is true)
                {
                    return VendorSdkLoader.Invoke(envoySystem, "getDevicePath", F53DeviceName) as string;
                }
            }
        }

        return null;
    }

    private static bool IsF53(object deviceInformation) =>
        string.Equals(
            VendorSdkLoader.Invoke(deviceInformation, "getDeviceType")?.ToString(),
            F53DeviceName,
            StringComparison.Ordinal);

    private static void TryLegacyInit(IReadOnlyList<Assembly> assemblies, object device)
    {
        try
        {
            Type deviceTypeEnum = VendorSdkLoader.FindType(assemblies, "com.arca.envoy.api.enumtypes.DeviceType", SdkName);
            Type currencyCode = VendorSdkLoader.FindType(assemblies, "com.arca.envoy.api.currency.CurrencyCode", SdkName);
            Type mediaMappings = VendorSdkLoader.FindType(
                assemblies, "com.arca.envoy.api.currency.FujitsuDefaultMediaMappings", SdkName);

            object? f53 = VendorSdkLoader.GetStaticField(deviceTypeEnum, F53DeviceName);
            object? usd = VendorSdkLoader.GetStaticField(currencyCode, "USD");
            if (f53 is not null && usd is not null)
            {
                object? mapping = VendorSdkLoader.InvokeStatic(mediaMappings, "getMapping", f53, usd);
                if (mapping is not null)
                {
                    VendorSdkLoader.Invoke(device, "setMediaMappings", mapping);
                }
            }

            Type billParams = VendorSdkLoader.FindType(assemblies, "com.arca.envoy.api.iface.FujitsuBillParams", SdkName);
            byte[] lengths = [0x9A, 0x9A, 0x9A];
            byte[] thicknesses = [0x0D, 0x0D, 0x0D];
            object? prm = Activator.CreateInstance(billParams, lengths, thicknesses, false);
            if (prm is not null)
            {
                VendorSdkLoader.Invoke(device, "mechanicalReset", prm);
            }
        }
        catch (Exception ex) when (ex is DeviceConnectFailedException
            or MissingMethodException
            or MissingMemberException
            or TargetInvocationException
            or ArgumentException)
        {
            // Best-effort parity only — see ConnectCoreAsync remark.
        }
    }

    private Dictionary<int, int> ReadCassetteAvailability()
    {
        object device = _device ?? throw new InvalidOperationException("dispenser_not_connected");
        object status = VendorSdkLoader.Invoke(device, "deviceStatus")
            ?? throw new InvalidOperationException("f53_status_unavailable");

        int errorCode = Convert.ToInt32(
            VendorSdkLoader.Invoke(status, "getErrorCode"), System.Globalization.CultureInfo.InvariantCulture);
        if (errorCode != 0)
        {
            throw new InvalidOperationException($"f53_status_error:{errorCode}");
        }

        int cassetteCount = Convert.ToInt32(
            VendorSdkLoader.Invoke(status, "getCassetteCount"), System.Globalization.CultureInfo.InvariantCulture);
        Dictionary<int, int> availability = [];
        lock (_cassetteLock)
        {
            _cassetteIndexByDenomination.Clear();
            for (int i = 1; i <= cassetteCount; i++)
            {
                object? info = VendorSdkLoader.Invoke(status, "getCassetteInfo", i);
                if (info is null || VendorSdkLoader.Invoke(info, "isCassettePresent") is not true)
                {
                    continue;
                }

                if (VendorSdkLoader.Invoke(status, "isCassettePickSensorBlocked", i) is true)
                {
                    continue;
                }

                object? media = VendorSdkLoader.Invoke(info, "getMedia");
                if (media is null)
                {
                    continue;
                }

                int denomination = Convert.ToInt32(
                    VendorSdkLoader.Invoke(media, "getValue"), System.Globalization.CultureInfo.InvariantCulture);
                if (denomination <= 0 || !_options.Denominations.Contains(denomination))
                {
                    continue;
                }

                // First matching cassette per denomination, legacy CassetteIndexForDenom parity.
                _cassetteIndexByDenomination.TryAdd(denomination, i);

                // The F53 reports presence, not note counts — the fleet's true counts live in
                // the cloud ledger; the configured bound stops edge over-commitment.
                availability[denomination] = availability.TryGetValue(denomination, out int existing)
                    ? existing + _options.AssumedCassetteBillCount
                    : _options.AssumedCassetteBillCount;
            }
        }

        return availability;
    }

    private DispenseResult DispenseCore(BillMixPlan plan)
    {
        object device = _device ?? throw new InvalidOperationException("dispenser_not_connected");
        Type hashMapType = _hashMapType ?? throw new InvalidOperationException("dispenser_not_connected");
        Type javaInteger = _javaIntegerType ?? throw new InvalidOperationException("dispenser_not_connected");
        Type prmType = _dispensePrmType ?? throw new InvalidOperationException("dispenser_not_connected");

        Dictionary<int, int> positions;
        lock (_cassetteLock)
        {
            // The cassette map is refreshed by every PlanAsync/ProbeAsync; an empty map means
            // the caller skipped planning — fail closed and force a re-plan.
            positions = [];
            foreach ((int denomination, int count) in plan.Bills)
            {
                if (!_cassetteIndexByDenomination.TryGetValue(denomination, out int cassetteIndex))
                {
                    return new DispenseResult(
                        false, new Dictionary<int, int>(), $"cassette_missing_for_denomination:{denomination}");
                }

                positions[denomination] = cassetteIndex;
            }
        }

        object posToCount = Activator.CreateInstance(hashMapType)!;
        foreach ((int denomination, int cassetteIndex) in positions)
        {
            VendorSdkLoader.Invoke(
                posToCount,
                "put",
                VendorSdkLoader.InvokeStatic(javaInteger, "valueOf", cassetteIndex),
                VendorSdkLoader.InvokeStatic(javaInteger, "valueOf", plan.Bills[denomination]));
        }

        object prm = Activator.CreateInstance(prmType, posToCount)!;
        object response = VendorSdkLoader.Invoke(device, "dispenseByPosition", prm)
            ?? throw new InvalidOperationException("f53_dispense_no_response");

        object common = VendorSdkLoader.Invoke(response, "getFujCommonRsp")
            ?? throw new InvalidOperationException("f53_dispense_no_status");
        int errorCode = Convert.ToInt32(
            VendorSdkLoader.Invoke(common, "getErrorCode"), System.Globalization.CultureInfo.InvariantCulture);

        Dictionary<int, int> dispensed = [];
        bool success = errorCode == 0;
        string? failureReason = success ? null : $"f53_error:{errorCode}";
        foreach ((int denomination, int cassetteIndex) in positions)
        {
            int dispensedCount = Convert.ToInt32(
                VendorSdkLoader.Invoke(response, "getDispensedByPosition", cassetteIndex),
                System.Globalization.CultureInfo.InvariantCulture);
            dispensed[denomination] = dispensedCount;
            if (dispensedCount != plan.Bills[denomination])
            {
                success = false;
                failureReason ??= "f53_dispense_count_mismatch";
            }
        }

        if (errorCode == 0)
        {
            for (int position = 1; position <= 6; position++)
            {
                int rejected = Convert.ToInt32(
                    VendorSdkLoader.Invoke(response, "getRejectedByPosition", position),
                    System.Globalization.CultureInfo.InvariantCulture);
                if (rejected > 0)
                {
                    success = false;
                    failureReason ??= $"f53_notes_rejected:{position}";
                }
            }
        }

        return new DispenseResult(success, dispensed, success ? null : failureReason);
    }
}
