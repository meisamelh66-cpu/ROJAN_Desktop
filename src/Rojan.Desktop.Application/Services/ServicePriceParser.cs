using System.Globalization;

namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Services' own copy of the string-money parsing <see cref="ServiceSearchFilter.MinPrice"/>/
/// <see cref="ServiceSearchFilter.MaxPrice"/> need before they can compare
/// against <see cref="Domain.Services.Service.Price"/> - the same shape as
/// <c>Reporting.MoneyParser</c>/<c>Accounting.AccountingMapper.ParseMoney</c>,
/// each a deliberate, documented duplicate per vertical slice rather than
/// a cross-module dependency (see <c>Reporting.MoneyParser</c>'s own doc
/// comment for the full reasoning) - Services reaching into Reporting's
/// internals for this would invert the intended "Reporting reads from
/// every module" direction, not the other way around.
/// </summary>
internal static class ServicePriceParser
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
