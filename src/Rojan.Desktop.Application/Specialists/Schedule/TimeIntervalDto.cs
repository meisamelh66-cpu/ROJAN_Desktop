namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Application-layer shape of a time-of-day window, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Schedule.TimeInterval"/>.</summary>
public sealed record TimeIntervalDto(TimeSpan Start, TimeSpan End);
