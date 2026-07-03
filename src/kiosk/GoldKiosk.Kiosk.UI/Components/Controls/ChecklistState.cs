namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>The visual state of a checklist row.</summary>
public enum ChecklistState
{
    /// <summary>Not started; dim outline disc.</summary>
    Pending,

    /// <summary>In progress; glowing cyan outline.</summary>
    Active,

    /// <summary>Completed; filled cyan disc with a check.</summary>
    Done,

    /// <summary>Failed; attention-colored disc with a cross.</summary>
    Failed,
}
