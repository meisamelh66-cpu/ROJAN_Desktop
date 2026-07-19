namespace Rojan.Desktop.Application.HR;

public sealed record CreateShiftRequest(
    string Label,
    Department Department,
    TimeSpan StartTime,
    TimeSpan EndTime);
