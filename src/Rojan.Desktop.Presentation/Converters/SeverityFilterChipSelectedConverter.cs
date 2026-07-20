using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Presentation.ViewModels.Notifications;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase 27: compares a severity filter chip's own option against the Notification Center's currently-selected one, for the chip row's "which one is active" highlight - a plain equality check needs a <see cref="IMultiValueConverter"/> since it depends on two independent bindings (the chip's own <c>DataContext</c> and the panel's <c>SelectedSeverityFilter</c>).</summary>
public sealed class SeverityFilterChipSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        (values is [NotificationSeverityFilterOption option, NotificationSeverityFilterOption selected] && option == selected)
            ? "True"
            : "False";

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("SeverityFilterChipSelectedConverter is one-way (display only).");
}
