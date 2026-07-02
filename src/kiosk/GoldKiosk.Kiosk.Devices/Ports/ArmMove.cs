namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Named arm movements between the machine's physical stations.</summary>
public enum ArmMove
{
    /// <summary>Pick the item from the customer tray and place it on the scale.</summary>
    TrayToScale,

    /// <summary>Move the item from the scale onto the XRF analyser window.</summary>
    ScaleToAnalyser,

    /// <summary>Move the item from the analyser into the volume chamber.</summary>
    AnalyserToChamber,

    /// <summary>Move the item from the chamber into the bagging position.</summary>
    ChamberToBag,

    /// <summary>Return the item to the customer tray (decline / rejection path).</summary>
    ReturnToTray,

    /// <summary>Move the arm to its safe home pose.</summary>
    Home,
}
