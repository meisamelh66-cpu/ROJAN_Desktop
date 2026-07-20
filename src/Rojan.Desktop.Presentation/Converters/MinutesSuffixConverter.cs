using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Formats a duration-in-minutes value as "{value} {localized minutes
/// suffix}" (e.g. "60 دقیقه") for the single spot (ServicePage's
/// KPIValue) that needed the suffix baked into one string property
/// rather than a Run-composed TextBlock - everywhere else in the app
/// splits the number and the "min" word into separate Runs instead (see
/// Strings.Common_MinutesShort), which this mirrors.
/// </summary>
public sealed class MinutesSuffixConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : $"{value} {Strings.Common_MinutesShort}";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("MinutesSuffixConverter is one-way (display only).");
}
