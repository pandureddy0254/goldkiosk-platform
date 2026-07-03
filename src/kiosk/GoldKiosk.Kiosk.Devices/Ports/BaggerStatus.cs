namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Bagger operating status used for the new-transaction interlock.</summary>
public enum BaggerStatus
{
    /// <summary>Idle — a new transaction may start.</summary>
    Idle,

    /// <summary>Bagging cycle in progress — new transactions are locked out.</summary>
    Bagging,

    /// <summary>The bagger reported a fault; operator attention required.</summary>
    Faulted,
}
