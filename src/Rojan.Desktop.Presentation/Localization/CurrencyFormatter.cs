using System.Globalization;
using System.Text;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Default <see cref="ICurrencyFormatter"/> implementation - culture-aware in the sense Phase 19A's "NUMBER FORMAT" requirement means: the same amount renders with Latin, Persian, or Arabic digit glyphs on request, independent of which currency it is.</summary>
public sealed class CurrencyFormatter : ICurrencyFormatter
{
    private static readonly char[] PersianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
    private static readonly char[] ArabicIndicDigits = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];

    public string Format(decimal amount, Currency currency, NumberDigits digits = NumberDigits.Latin)
    {
        var number = amount.ToString("#,0.##", CultureInfo.InvariantCulture);
        number = ApplyDigits(number, digits);

        return currency switch
        {
            Currency.Toman => $"{number} تومان",
            Currency.Rial => $"{number} ﷼",
            Currency.Usd => $"${number}",
            Currency.Eur => $"€{number}",
            _ => number,
        };
    }

    private static string ApplyDigits(string number, NumberDigits digits)
    {
        if (digits == NumberDigits.Latin)
        {
            return number;
        }

        var glyphs = digits == NumberDigits.Persian ? PersianDigits : ArabicIndicDigits;
        var builder = new StringBuilder(number.Length);
        foreach (var character in number)
        {
            builder.Append(character is >= '0' and <= '9' ? glyphs[character - '0'] : character);
        }

        return builder.ToString();
    }
}
