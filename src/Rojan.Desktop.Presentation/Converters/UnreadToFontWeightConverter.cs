using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase 27: an unread notification's title renders <see cref="FontWeights.SemiBold"/>, a read one <see cref="FontWeights.Normal"/> - the Read/Unread state's one visual cue besides the unread-dot each row's severity icon area implies.</summary>
public sealed class UnreadToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? FontWeights.Normal : FontWeights.SemiBold;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("UnreadToFontWeightConverter is one-way (display only).");
}
