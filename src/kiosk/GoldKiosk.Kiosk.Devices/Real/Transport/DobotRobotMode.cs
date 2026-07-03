namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>Dobot CR robot modes from the feedback stream (legacy <c>FeedbackData</c> constants).</summary>
internal enum DobotRobotMode
{
    /// <summary>No controller reachable.</summary>
    NoController = -1,

    /// <summary>Powered but not initialized.</summary>
    Init = 1,

    /// <summary>Brake released.</summary>
    BrakeOpen = 2,

    /// <summary>Servos disabled.</summary>
    Disabled = 4,

    /// <summary>Enabled and idle — the mode that signals motion completion.</summary>
    Enabled = 5,

    /// <summary>Backdrive (hand-guide) mode.</summary>
    Backdrive = 6,

    /// <summary>Executing a motion command.</summary>
    Running = 7,

    /// <summary>Trajectory recording.</summary>
    Recording = 8,

    /// <summary>Alarm state — motion has failed.</summary>
    Error = 9,

    /// <summary>Paused mid-trajectory.</summary>
    Paused = 10,

    /// <summary>Jogging.</summary>
    Jog = 11,
}
