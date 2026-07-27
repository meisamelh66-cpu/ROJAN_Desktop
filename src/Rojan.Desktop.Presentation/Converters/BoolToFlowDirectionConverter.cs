using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Bool-to-<see cref="FlowDirection"/> - for the Language Selector's dropdown rows, each bound to its own <see cref="Localization.LanguageInfo.IsRightToLeft"/> so every row reads in its own language's natural direction (flag/name order), independent of whichever language is currently active for the rest of the app.</summary>
public sealed class BoolToFlowDirectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("BoolToFlowDirectionConverter is one-way (display only).");
}
