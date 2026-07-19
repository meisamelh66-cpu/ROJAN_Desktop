using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Generic UI-only helper for step-indicator style controls: true when the
/// bound enum value's ordinal position is at or past the named threshold
/// given as <c>ConverterParameter</c> (e.g. binding
/// <c>BookingWizardStep.CurrentStep</c> with parameter "Service" is true
/// once the wizard has reached or passed the Service step). Pure
/// Presentation-layer XAML plumbing - no workflow/business rule, nothing
/// this depends on or produces crosses into Application/Domain.
/// </summary>
public sealed class EnumAtLeastConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string thresholdName)
        {
            return false;
        }

        var thresholdValue = Enum.Parse(value.GetType(), thresholdName);
        return System.Convert.ToInt32(value, culture) >= System.Convert.ToInt32(thresholdValue, culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("EnumAtLeastConverter is one-way (display only).");
}
