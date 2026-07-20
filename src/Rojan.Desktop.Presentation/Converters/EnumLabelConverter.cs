using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Displays any Domain status/type/method enum (CustomerStatus,
/// BookingStatus, InvoiceStatus, PaymentMethod, StockTransactionType, ...)
/// through <see cref="Strings.GetEnumLabel"/> instead of its raw
/// <c>ToString()</c> member name, which is what a bare <c>{Binding
/// Status}</c> renders and is always English regardless of the selected
/// language. One shared converter for every such enum in the app, keyed by
/// member name (e.g. "Active", "Cancelled") rather than per enum type,
/// since Presentation has no reason to special-case each Domain enum
/// individually just to localize its display text.
/// </summary>
public sealed class EnumLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : Strings.GetEnumLabel(value.ToString() ?? string.Empty);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("EnumLabelConverter is one-way (display only).");
}
