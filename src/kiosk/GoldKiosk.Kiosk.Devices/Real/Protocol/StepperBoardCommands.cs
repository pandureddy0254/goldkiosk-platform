namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// The stepper board's two-letter ASCII command set (legacy <c>ClientConfigProvider</c>
/// defaults). Every command is written as a line and acknowledged with a single
/// <see cref="Ack"/> character once the motion completes.
/// </summary>
internal static class StepperBoardCommands
{
    /// <summary>Open (lower) the customer tray (legacy <c>OpenTrayCmd</c>).</summary>
    internal const string OpenTray = "ld";

    /// <summary>Close (raise) the customer tray (legacy <c>CloseTrayCmd</c>).</summary>
    internal const string CloseTray = "lu";

    /// <summary>Seal the volume chamber (legacy <c>ChamberSealCmd</c>).</summary>
    internal const string ChamberSeal = "cu";

    /// <summary>Open the volume chamber (legacy <c>ChamberOpenCmd</c>).</summary>
    internal const string ChamberOpen = "cd";

    /// <summary>Raise the compression piston (legacy <c>PistonUpCmd</c>).</summary>
    internal const string PistonUp = "pu";

    /// <summary>Lower the compression piston (legacy <c>PistonDownCmd</c>).</summary>
    internal const string PistonDown = "pd";

    /// <summary>Command-complete acknowledgement character.</summary>
    internal const char Ack = '*';
}
