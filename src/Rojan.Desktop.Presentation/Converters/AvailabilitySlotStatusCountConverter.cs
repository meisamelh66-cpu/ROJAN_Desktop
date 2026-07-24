using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Counts how many <see cref="AvailabilitySlotDto"/> entries in the bound
/// list have the <see cref="AvailabilityStatus"/> named by
/// <c>ConverterParameter</c> ("Available"/"Booked"/"Unavailable") - the
/// Week view's per-day status-count StatusPills (Sprint 2 Commit 5), a
/// pure display aggregation kept in Presentation's Converters, not pushed
/// into the ViewModel or the Application-layer DTO.
/// </summary>
public sealed class AvailabilitySlotStatusCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<AvailabilitySlotDto> slots
            || parameter is not string statusName
            || !Enum.TryParse<AvailabilityStatus>(statusName, out var status))
        {
            return 0;
        }

        return slots.Count(slot => slot.Status == status);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("AvailabilitySlotStatusCountConverter is one-way (display only).");
}
