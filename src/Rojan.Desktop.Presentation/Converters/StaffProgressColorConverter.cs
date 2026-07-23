using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Phase C-2 (Staff Status Panel): colors each ring purely from its own
/// ViewModels.Dashboard.StaffStatusItem.ProgressPercentage value - no
/// separate status field to keep in sync, no fabricated business meaning
/// beyond "how far along is this value." Reuses the existing Success/Warning/Error/
/// MutedText semantic brushes (the same ones TrendIndicator already uses),
/// not new colors.
/// </summary>
public sealed class StaffProgressColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percentage = value is int intValue ? intValue : 0;
        var key = percentage switch
        {
            <= 0 => "Rojan.Brush.MutedText",
            >= 100 => "Rojan.Brush.Success",
            >= 60 => "Rojan.Brush.Warning",
            _ => "Rojan.Brush.Error",
        };

        return System.Windows.Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("StaffProgressColorConverter is one-way (display only).");
}
