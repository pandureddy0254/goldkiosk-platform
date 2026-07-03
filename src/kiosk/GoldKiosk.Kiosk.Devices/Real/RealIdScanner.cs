using System.Reflection;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real ID scanner driver, reflection-gated over the fitted vendor SDK selected by
/// <c>Devices:Overrides:id_scanner:Connection:Variant</c>: <c>acuant</c> (ScanShell via
/// <c>Interop.ScanWLightLib.dll</c>, licence-key init per the legacy <c>GCIdScanner</c>) or
/// <c>gemalto</c> (3M/Gemalto full-page reader via <c>MMMReaderDotNet40.dll</c>). SDK absent
/// → <c>vendor_sdk_missing:*</c> fault at connect. The scan invocation and field extraction
/// are duck-typed best-effort (the legacy field parse ran through the CSSN web-service SDK);
/// unmatched bindings return a failed <see cref="IdScanResult"/> with a machine-readable
/// reason for Phase-4 hardware-lab completion — scan output is restricted PII and is never
/// logged or placed in health details.
/// </summary>
public sealed class RealIdScanner : RealDeviceBase, IIdScanner
{
    private static readonly string[] _scanMemberCandidates = ["ScanDocument", "ReadDocument", "Scan"];
    private static readonly string[] _firstNameCandidates = ["FirstName", "GivenName", "NameFirst", "Forename"];
    private static readonly string[] _lastNameCandidates = ["LastName", "Surname", "NameLast", "FamilyName"];
    private static readonly string[] _dateOfBirthCandidates = ["DateOfBirth", "BirthDate", "DOB"];
    private static readonly string[] _expiryCandidates = ["ExpiresOn", "ExpirationDate", "ExpiryDate", "Expiration"];
    private static readonly string[] _documentNumberCandidates = ["DocumentNumber", "LicenseNumber", "IdNumber", "Number"];
    private static readonly string[] _portraitCandidates = ["PortraitImage", "Portrait", "Photo", "FaceImage"];

    private readonly ConnectionOptions _connection;
    private readonly IdScannerOptions _options;
    private object? _sdkInstance;

    /// <summary>Initializes the driver with legacy-parity defaults (Acuant variant).</summary>
    public RealIdScanner()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (variant, SDK path); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Scanner tuning (licence key, Gemalto SDK path); <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealIdScanner(
        ConnectionOptions? connection,
        IdScannerOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.IdScanner, "ID scanner (Acuant / Gemalto)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.IdScanner));
        _options = options ?? new IdScannerOptions();
    }

    private bool IsGemalto =>
        string.Equals(_connection.Variant, RealDeviceDefaults.IdScannerVariantGemalto, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IdScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        object sdk = _sdkInstance ?? throw new InvalidOperationException("id_scanner_not_connected");
        SetHealth(DeviceState.Busy, "Scanning document.");
        try
        {
            IdScanResult result = await Task.Run(() => RunScan(sdk), cancellationToken).ConfigureAwait(false);
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
            SetHealth(DeviceState.Faulted, $"id_scan_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            if (IsGemalto)
            {
                Assembly sdk = VendorSdkLoader.LoadAssembly(_options.GemaltoAssemblyPath, "MMMReaderDotNet40");
                Type readerType = VendorSdkLoader.GetRequiredType(sdk, "MMM.Readers.FullPage.MMMReader", "MMMReaderDotNet40");
                _sdkInstance = Activator.CreateInstance(readerType)
                    ?? throw new DeviceConnectFailedException(VendorSdkLoader.MissingDetail("MMMReaderDotNet40"));
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.AcuantLicenseKey))
            {
                throw new DeviceConnectFailedException("acuant_license_missing");
            }

            Assembly acuant = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, "Interop.ScanWLightLib");
            Type slibType = acuant.GetType("ScanWLightLib.SlibClass", throwOnError: false)
                ?? VendorSdkLoader.GetRequiredType(acuant, "ScanWLightLib.Slib", "Interop.ScanWLightLib");
            object slib = Activator.CreateInstance(slibType)
                ?? throw new DeviceConnectFailedException(VendorSdkLoader.MissingDetail("Interop.ScanWLightLib"));

            // Legacy GCIdScanner parity: -13 ("already initialized") is benign.
            int initResult = Convert.ToInt32(
                VendorSdkLoader.Invoke(slib, "InitLibrary", _options.AcuantLicenseKey),
                System.Globalization.CultureInfo.InvariantCulture);
            if (initResult < 0 && initResult != -13)
            {
                throw new DeviceConnectFailedException($"acuant_init_failed:{initResult}");
            }

            _sdkInstance = slib;
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseSdk();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseSdk();
        }

        base.Dispose(disposing);
    }

    private static IdScanResult RunScan(object sdk)
    {
        object? scanOutput = null;
        foreach (string member in _scanMemberCandidates)
        {
            try
            {
                scanOutput = VendorSdkLoader.Invoke(sdk, member);
                break;
            }
            catch (MissingMethodException)
            {
                // Try the next candidate member name.
            }
        }

        if (scanOutput is null)
        {
            return new IdScanResult(false, "id_scan_binding_pending_hardware_validation", null);
        }

        return TryExtractDocument(scanOutput, out IdDocument? document)
            ? new IdScanResult(true, null, document)
            : new IdScanResult(false, "id_scan_fields_unmapped", null);
    }

    private static bool TryExtractDocument(object scanOutput, out IdDocument? document)
    {
        document = null;
        string? firstName = ReadString(scanOutput, _firstNameCandidates);
        string? lastName = ReadString(scanOutput, _lastNameCandidates);
        DateOnly? dateOfBirth = ReadDate(scanOutput, _dateOfBirthCandidates);
        DateOnly? expiresOn = ReadDate(scanOutput, _expiryCandidates);
        string? documentNumber = ReadString(scanOutput, _documentNumberCandidates);
        byte[]? portrait = ReadBytes(scanOutput, _portraitCandidates);

        if (firstName is null || lastName is null || dateOfBirth is null || expiresOn is null || documentNumber is null)
        {
            return false;
        }

        document = new IdDocument(
            firstName,
            lastName,
            dateOfBirth.Value,
            expiresOn.Value,
            documentNumber,
            IsGovernmentId: true,
            portrait ?? []);
        return true;
    }

    private static string? ReadString(object source, string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (ReadMember(source, candidate) is string { Length: > 0 } value)
            {
                return value;
            }
        }

        return null;
    }

    private static DateOnly? ReadDate(object source, string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            object? value = ReadMember(source, candidate);
            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }

            if (value is string text && DateOnly.TryParse(
                    text, System.Globalization.CultureInfo.InvariantCulture, out DateOnly parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static byte[]? ReadBytes(object source, string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (ReadMember(source, candidate) is byte[] { Length: > 0 } bytes)
            {
                return bytes;
            }
        }

        return null;
    }

    private static object? ReadMember(object source, string name)
    {
        try
        {
            return VendorSdkLoader.GetProperty(source, name);
        }
        catch (MissingMemberException)
        {
            return null;
        }
    }

    private void ReleaseSdk()
    {
        if (_sdkInstance is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _sdkInstance = null;
    }
}
