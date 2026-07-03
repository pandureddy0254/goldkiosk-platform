using System.Globalization;
using System.Runtime.InteropServices;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real Brother b-PAC label printer driver: late-bound COM over ProgID
/// <c>bpac.Document</c> printing the legacy <c>ShipForm.lbx</c> template. Field mapping per
/// the legacy <c>BrotherPrinter.Print</c>: <c>Text01</c> invoice, <c>Text02</c> bag number,
/// <c>Text03</c> weight, <c>Text04</c> offer display; printed with auto-cut. b-PAC not
/// registered → <c>vendor_sdk_missing:bpac.Document</c> at connect.
/// </summary>
public sealed class RealLabelPrinter : RealDeviceBase, ILabelPrinter
{
    /// <summary>b-PAC <c>PrintOptionConstants.bpoAutoCut</c>.</summary>
    private const int BpoAutoCut = 0x1;

    private readonly LabelPrinterOptions _options;
    private Type? _documentType;

    /// <summary>Initializes the driver with legacy-parity defaults (ShipForm.lbx).</summary>
    public RealLabelPrinter()
        : this(null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="options">Template path and ProgID; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealLabelPrinter(LabelPrinterOptions? options, TimeProvider? timeProvider = null)
        : base(DeviceKeys.LabelPrinter, "Label printer (Brother b-PAC)", isCritical: false, timeProvider)
    {
        _options = options ?? new LabelPrinterOptions();
    }

    /// <inheritdoc />
    public async Task PrintAsync(BagLabel label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(label);
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Printing label.");
        try
        {
            await Task.Run(() => PrintCore(label), cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"label_print_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            _documentType = VendorSdkLoader.GetComType(_options.ProgId);
            if (!File.Exists(ResolveTemplatePath()))
            {
                throw new DeviceConnectFailedException($"label_template_missing:{_options.TemplatePath}");
            }
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        _documentType = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_documentType is null)
        {
            return Task.FromResult(new DeviceProbeResult(false, 0, Health.Detail ?? "Printer is not connected."));
        }

        return Task.FromResult(File.Exists(ResolveTemplatePath())
            ? new DeviceProbeResult(true, 0, "b-PAC registered and template present.")
            : new DeviceProbeResult(false, 0, $"label_template_missing:{_options.TemplatePath}"));
    }

    private string ResolveTemplatePath() =>
        Path.IsPathRooted(_options.TemplatePath)
            ? _options.TemplatePath
            : Path.Combine(AppContext.BaseDirectory, _options.TemplatePath);

    private void PrintCore(BagLabel label)
    {
        Type documentType = _documentType ?? throw new InvalidOperationException("label_printer_not_connected");
        object document = Activator.CreateInstance(documentType)
            ?? throw new InvalidOperationException("bpac_document_create_failed");
        try
        {
            if (VendorSdkLoader.Invoke(document, "Open", ResolveTemplatePath()) is not true)
            {
                throw new InvalidOperationException("label_template_open_failed");
            }

            SetTemplateText(document, "Text01", label.InvoiceNumber);
            SetTemplateText(document, "Text02", label.BagNumber);
            SetTemplateText(document, "Text03", label.WeightGrams.ToString("0.####", CultureInfo.InvariantCulture));
            SetTemplateText(document, "Text04", label.OfferDisplay);

            object? printer = VendorSdkLoader.GetProperty(document, "Printer");
            if (printer is not null && VendorSdkLoader.Invoke(printer, "GetMediaId") is { } mediaId)
            {
                VendorSdkLoader.Invoke(document, "SetMediaById", mediaId, false);
            }

            VendorSdkLoader.Invoke(document, "StartPrint", string.Empty, BpoAutoCut);
            VendorSdkLoader.Invoke(document, "PrintOut", 1, BpoAutoCut);
            VendorSdkLoader.Invoke(document, "EndPrint");
            VendorSdkLoader.Invoke(document, "Close");
        }
        finally
        {
            if (Marshal.IsComObject(document))
            {
                Marshal.ReleaseComObject(document);
            }
        }
    }

    private static void SetTemplateText(object document, string objectName, string value)
    {
        object templateObject = VendorSdkLoader.Invoke(document, "GetObject", objectName)
            ?? throw new InvalidOperationException($"label_template_object_missing:{objectName}");
        VendorSdkLoader.SetProperty(templateObject, "Text", value);
    }
}
