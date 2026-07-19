using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Localization;

public sealed class CurrencyFormatterTests
{
    private readonly CurrencyFormatter _formatter = new();

    [Theory]
    [InlineData(Currency.Toman, "1,250 تومان")]
    [InlineData(Currency.Rial, "1,250 ﷼")]
    [InlineData(Currency.Usd, "$1,250")]
    [InlineData(Currency.Eur, "€1,250")]
    public void Format_WithLatinDigits_AppliesCorrectCurrencyGlyph(Currency currency, string expected)
    {
        var result = _formatter.Format(1250m, currency, NumberDigits.Latin);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_WithPersianDigits_SubstitutesPersianDigitGlyphs()
    {
        var result = _formatter.Format(1250m, Currency.Toman, NumberDigits.Persian);

        Assert.Equal("۱,۲۵۰ تومان", result);
    }

    [Fact]
    public void Format_WithArabicDigits_SubstitutesArabicIndicDigitGlyphs()
    {
        var result = _formatter.Format(1250m, Currency.Usd, NumberDigits.Arabic);

        Assert.Equal("$١,٢٥٠", result);
    }

    [Fact]
    public void Format_DefaultsToLatinDigits_WhenNotSpecified()
    {
        var result = _formatter.Format(42m, Currency.Usd);

        Assert.Equal("$42", result);
    }
}
