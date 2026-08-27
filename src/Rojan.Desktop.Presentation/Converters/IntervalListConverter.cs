using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Phase 7.2.6 Shift Engine UI Activation - renders an
/// <see cref="IReadOnlyList{T}"/> of <see cref="TimeIntervalDto"/> as
/// <c>"09:00-13:00, 14:00-18:00"</c>, or a fixed "unavailable" marker for
/// an empty list - the real, meaningful "unavailable all day" state a
/// <see cref="Rojan.Desktop.Domain.Specialists.Schedule.ScheduleOverride"/>
/// can carry, not a display bug. Display-only formatting, no business
/// rule - the empty-means-unavailable meaning itself lives in Domain/
/// Application's own doc comments, this converter only renders it.
/// </summary>
public sealed class IntervalListConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<TimeIntervalDto> intervals || intervals.Count == 0)
        {
            return Strings.Specialists_Schedule_Unavailable;
        }

        return string.Join(", ", intervals.Select(interval => $"{Format(interval.Start)}-{Format(interval.End)}"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("IntervalListConverter is one-way (display only).");

    private static string Format(TimeSpan value) => value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}
