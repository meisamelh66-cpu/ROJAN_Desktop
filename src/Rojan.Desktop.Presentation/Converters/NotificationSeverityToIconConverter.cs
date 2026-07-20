using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Maps a <see cref="NotificationSeverity"/> directly to its Fluent icon
/// glyph character - a converter cannot resolve a <c>{StaticResource}</c>
/// key bound at runtime (resource keys in XAML must be literal), so this
/// mirrors <c>Themes/Icons.xaml</c>'s <c>Rojan.Icon.CheckCircle</c>/
/// <c>Warning</c>/<c>ErrorCircle</c>/<c>Information</c> codepoints
/// directly (as numeric char literals, to avoid any encoding ambiguity
/// with raw private-use-area Unicode characters in source) - keep both
/// in sync if either changes.
/// </summary>
public sealed class NotificationSeverityToIconConverter : IValueConverter
{
    private const char CheckCircleGlyph = (char)0xE930;
    private const char WarningGlyph = (char)0xE7BA;
    private const char ErrorCircleGlyph = (char)0xE783;
    private const char InformationGlyph = (char)0xE946;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        NotificationSeverity.Success => CheckCircleGlyph.ToString(),
        NotificationSeverity.Warning => WarningGlyph.ToString(),
        NotificationSeverity.Error => ErrorCircleGlyph.ToString(),
        _ => InformationGlyph.ToString(),
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("NotificationSeverityToIconConverter is one-way (display only).");
}
