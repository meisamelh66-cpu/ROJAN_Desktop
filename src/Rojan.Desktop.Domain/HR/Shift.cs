namespace Rojan.Desktop.Domain.HR;

/// <summary>A reusable shift template (e.g. "Morning - Hair") that <see cref="ShiftAssignment"/> schedules an employee onto for a specific date.</summary>
public sealed record Shift(
    string Id,
    string Label,
    Department Department,
    TimeSpan StartTime,
    TimeSpan EndTime);
