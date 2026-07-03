namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Olympus Vanta WebSocket command ids (legacy <c>VantaCommandRequestResponseId</c>).</summary>
public enum VantaCommandId
{
    /// <summary>Login with user id + password.</summary>
    Login = 301,

    /// <summary>Logout of the controller session.</summary>
    Logout = 317,

    /// <summary>Start an analysis exposure.</summary>
    StartTest = 601,

    /// <summary>Stop the running exposure.</summary>
    StopTest = 602,

    /// <summary>Start a calibration check.</summary>
    StartCalCheck = 606,

    /// <summary>Asynchronous notification envelope (results, battery, errors).</summary>
    Notification = 403,

    /// <summary>Query device faults.</summary>
    GetFaults = 253,

    /// <summary>Activate an analysis method (e.g. <c>preciousMetal-VLW</c>).</summary>
    SetCurrentMethod = 703,

    /// <summary>List available analysis methods.</summary>
    GetMethodList = 701,

    /// <summary>Shut the gun down.</summary>
    ShutDown = 405,
}
