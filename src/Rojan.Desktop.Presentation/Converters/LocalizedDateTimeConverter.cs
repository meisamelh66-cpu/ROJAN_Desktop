using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// UX audit fix: formats a <see cref="DateTimeOffset"/> using
/// <see cref="CultureInfo.CurrentCulture"/> explicitly - the same
/// "existing Persian localization system" <c>Shell.MainWindowViewModel</c>
/// already establishes for the header clock (<c>DateText</c>/<c>TimeText</c>,
/// set via <c>Thread.CurrentThread.CurrentCulture</c> at startup). A plain
/// XAML <c>StringFormat</c> binding does not use this session culture - it
/// resolves through <see cref="System.Windows.FrameworkElement.Language"/>
/// instead, which defaults to en-US regardless of the active language,
/// which is why booking dates were rendering as "Jul 28, 2026" inside an
/// otherwise fully Persian, RTL screen. <c>parameter</c> is the .NET custom
/// date/time format string to use (e.g. "MMM d, yyyy t"); the binding's own
/// <c>culture</c> argument is deliberately not used, for the same reason.
/// </summary>
public sealed class LocalizedDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(parameter as string, CultureInfo.CurrentCulture),
            DateTime dateTime => dateTime.ToString(parameter as string, CultureInfo.CurrentCulture),
            _ => string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("LocalizedDateTimeConverter is one-way (display only).");
}
