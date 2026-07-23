using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase C-2 (Staff Status Panel): the avatar placeholder's single letter - no photo assets, so this is the honest "no external assets" stand-in every other avatar-less list in this app already uses (e.g. the header's own guest-user circle).</summary>
public sealed class StaffInitialConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string { Length: > 0 } name ? name[..1] : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("StaffInitialConverter is one-way (display only).");
}
