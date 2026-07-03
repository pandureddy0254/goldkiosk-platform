using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Legacy-parity connection defaults for every real driver, harvested from the GoldCube
/// legacy repos (Client.BusinessLogic <c>Rs232Options</c> attributes, GoldCube.Store
/// <c>ConfigProvider</c>). A kiosk provisioned with an empty <c>Connection</c> section talks
/// to the same ports/hosts the legacy machine wiring used. Every value is overridable per
/// device via <c>Devices:Overrides:{deviceKey}:Connection</c>.
/// </summary>
public static class RealDeviceDefaults
{
    /// <summary>Scale serial port (legacy <c>Scale.cs</c>: COM5, 9600 8-N-1).</summary>
    public const string ScalePort = "COM5";

    /// <summary>Scale baud rate.</summary>
    public const int ScaleBaudRate = 9600;

    /// <summary>Pressure sensor serial port (legacy <c>PressureSensor.cs</c>: COM4, 115200 8-N-1).</summary>
    public const string PressureSensorPort = "COM4";

    /// <summary>Pressure sensor baud rate.</summary>
    public const int PressureSensorBaudRate = 115200;

    /// <summary>Stepper board serial port (legacy <c>StepperBoard.cs</c>: COM3, 9600 8-N-1) — shared by tray and chamber.</summary>
    public const string StepperBoardPort = "COM3";

    /// <summary>Stepper board baud rate.</summary>
    public const int StepperBoardBaudRate = 9600;

    /// <summary>Dobot arm controller host (legacy <c>DobotManager.cs</c>).</summary>
    public const string DobotHost = "192.168.1.6";

    /// <summary>Dobot dashboard (ASCII command) TCP port.</summary>
    public const int DobotDashboardPort = 29999;

    /// <summary>Dobot motion command TCP port.</summary>
    public const int DobotMotionPort = 30003;

    /// <summary>Dobot real-time feedback TCP port (1440-byte packets).</summary>
    public const int DobotFeedbackPort = 30004;

    /// <summary>Kollmorgen AKD drive host for the analyser-positioning axis (legacy <c>AKD.AmpsLocalAddressZ</c>).</summary>
    public const string AkdHost = "192.168.0.11";

    /// <summary>Kollmorgen AKD telnet port.</summary>
    public const int AkdPort = 23;

    /// <summary>Olympus Vanta XRF gun host (legacy <c>VantaHost</c>).</summary>
    public const string VantaHost = "192.168.7.2";

    /// <summary>Vanta WebSocket command port (legacy <c>VantaPort</c>).</summary>
    public const int VantaWebSocketPort = 7860;

    /// <summary>Vanta UDP heartbeat port (legacy <c>VantaUDPPort</c>).</summary>
    public const int VantaUdpPort = 7862;

    /// <summary>Advantech DAQ SDK assembly file name (legacy <c>HelperDlls\Automation.BDaq.dll</c>).</summary>
    public const string AdvantechAssembly = "Automation.BDaq.dll";

    /// <summary>Advantech device description used to select the DAQ board (legacy <c>DoInput</c>/<c>DiOutput</c>).</summary>
    public const string AdvantechDeviceDescription = "USB-4761,BID#0";

    /// <summary>ARCA Envoy API assembly file name (legacy <c>ExternalLib\ARCA\2.9.2\LibEnvoyAPI.dll</c>).</summary>
    public const string ArcaEnvoyAssembly = "LibEnvoyAPI.dll";

    /// <summary>ARCA Envoy RMI host (the Envoy service runs on the kiosk machine itself).</summary>
    public const string ArcaEnvoyHost = "localhost";

    /// <summary>Acuant LightSDK interop assembly file name (legacy <c>Interop.ScanWLightLib.dll</c>).</summary>
    public const string AcuantAssembly = "Interop.ScanWLightLib.dll";

    /// <summary>Gemalto/3M full-page reader assembly file name (legacy <c>MMMReaderDotNet40.dll</c>).</summary>
    public const string GemaltoAssembly = "MMMReaderDotNet40.dll";

    /// <summary>FlexCode fingerprint SDK interop assembly file name (legacy <c>Interop.FlexCodeSDK.dll</c>).</summary>
    public const string FlexCodeAssembly = "Interop.FlexCodeSDK.dll";

    /// <summary>DirectShow camera wrapper assembly file name (legacy <c>Camera_NET.dll</c>).</summary>
    public const string CameraNetAssembly = "Camera_NET.dll";

    /// <summary>Brother b-PAC COM ProgID (legacy <c>bpac.DocumentClass</c>).</summary>
    public const string BrotherBpacProgId = "bpac.Document";

    /// <summary>Metal analyser variant selecting the Olympus Vanta WebSocket driver.</summary>
    public const string AnalyserVariantVanta = "vanta";

    /// <summary>Metal analyser variant selecting the legacy Innov-X COM driver.</summary>
    public const string AnalyserVariantInnovX = "innovx";

    /// <summary>ID scanner variant selecting the Acuant ScanShell driver.</summary>
    public const string IdScannerVariantAcuant = "acuant";

    /// <summary>ID scanner variant selecting the Gemalto/3M full-page reader driver.</summary>
    public const string IdScannerVariantGemalto = "gemalto";

    /// <summary>
    /// Returns the default <see cref="ConnectionOptions"/> for a device key. Devices with a
    /// second physical link (the chamber's pressure sensor, the arm's AKD axis) expose those
    /// extras on their dedicated options classes, not here.
    /// </summary>
    /// <param name="deviceKey">Canonical device key from <see cref="DeviceKeys"/>.</param>
    /// <returns>The legacy-parity defaults; unknown keys get empty options.</returns>
    public static ConnectionOptions For(string deviceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceKey);

        return deviceKey switch
        {
            DeviceKeys.Scale => new ConnectionOptions { Port = ScalePort, BaudRate = ScaleBaudRate },
            DeviceKeys.Tray => new ConnectionOptions { Port = StepperBoardPort, BaudRate = StepperBoardBaudRate },
            DeviceKeys.VolumeChamber => new ConnectionOptions { Port = StepperBoardPort, BaudRate = StepperBoardBaudRate },
            DeviceKeys.RoboticArm => new ConnectionOptions { Host = DobotHost, TcpPort = DobotDashboardPort },
            DeviceKeys.MetalAnalyser => new ConnectionOptions
            {
                Host = VantaHost,
                TcpPort = VantaWebSocketPort,
                Variant = AnalyserVariantVanta,
            },
            DeviceKeys.SensorBoard => new ConnectionOptions { VendorAssemblyPath = AdvantechAssembly },
            DeviceKeys.PowerRelays => new ConnectionOptions { VendorAssemblyPath = AdvantechAssembly },
            DeviceKeys.IdScanner => new ConnectionOptions
            {
                VendorAssemblyPath = AcuantAssembly,
                Variant = IdScannerVariantAcuant,
            },
            DeviceKeys.FingerprintScanner => new ConnectionOptions { VendorAssemblyPath = FlexCodeAssembly },
            DeviceKeys.CashDispenser => new ConnectionOptions
            {
                VendorAssemblyPath = ArcaEnvoyAssembly,
                Host = ArcaEnvoyHost,
            },
            DeviceKeys.Camera => new ConnectionOptions { VendorAssemblyPath = CameraNetAssembly },
            _ => new ConnectionOptions(),
        };
    }
}
