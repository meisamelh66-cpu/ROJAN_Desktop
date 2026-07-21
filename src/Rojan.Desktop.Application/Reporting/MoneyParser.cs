using System.Globalization;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// Reporting's own copy of the string-money parsing every read-only
/// module's display-string price field needs before it can be summed
/// (<c>CustomerDto.LifetimeValue</c>, <c>BookingDto.Price</c>,
/// <c>ServiceDto.Price</c>, <c>ProductDto.UnitPrice</c>) - the same shape
/// as <c>Accounting.AccountingMapper.ParseMoney</c>, which is internal to
/// that assembly and not visible here, so this is a deliberate, documented
/// duplicate rather than a cross-module dependency. Accounting's and HR's
/// own DTOs are already <c>decimal</c> and never need this.
/// </summary>
internal static class MoneyParser
{
    public static decimal Parse(string value)
    {
        if (value.Contains("رایگان", StringComparison.Ordinal))
        {
            return 0m;
        }

        var trimmed = value.TrimStart('$').Replace("تومان", string.Empty, StringComparison.Ordinal).Replace("﷼", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }
}
