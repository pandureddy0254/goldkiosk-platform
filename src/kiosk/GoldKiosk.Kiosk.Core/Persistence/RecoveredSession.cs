namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// A non-terminal session journal found on disk at startup — a session interrupted by a
/// crash or restart, surfaced to the host for resume-or-safe-abort (ADR 0002).
/// </summary>
/// <param name="FolderPath">The absolute transaction folder path.</param>
/// <param name="Journal">The last journal snapshot written before the interruption.</param>
public sealed record RecoveredSession(string FolderPath, SessionJournal Journal);
