using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Maps a <see cref="NotificationSeverity"/> to its themed brush. A converter has no target element to call <see cref="FrameworkElement.TryFindResource(object)"/> on, so this resolves against <c>System.Windows.Application.Current</c>'s merged resources instead - the same theme dictionaries every View already merges.</summary>
public sealed class NotificationSeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            NotificationSeverity.Success => "Rojan.Brush.Success",
            NotificationSeverity.Warning => "Rojan.Brush.Warning",
            NotificationSeverity.Error => "Rojan.Brush.Error",
            _ => "Rojan.Brush.Accent",
        };

        return System.Windows.Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("NotificationSeverityToBrushConverter is one-way (display only).");
}
