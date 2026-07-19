namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a shift template, mapped from <see cref="Rojan.Desktop.Domain.HR.Shift"/> by <see cref="HrMapper"/>.</summary>
public sealed record ShiftDto(
    string Id,
    string Label,
    Department Department,
    TimeSpan StartTime,
    TimeSpan EndTime);
