using System.Globalization;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Settings;

namespace Rojan.Desktop.Presentation.Tests.Localization;

/// <summary>Exercises <see cref="CalendarService"/>'s provider-selection logic - the reason <see cref="PersianCalendarProvider"/> was previously unreachable (only <see cref="GregorianCalendarProvider"/> was ever registered) is exactly what this service now fixes.</summary>
public sealed class CalendarServiceTests
{
    private static readonly LanguageInfo Persian = new("fa-IR", "فارسی", "Persian", true, "Vazirmatn", NumberDigits.Persian, "Toman", "Persian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo English = new("en-US", "English", "English", false, "Segoe UI", NumberDigits.Latin, "Usd", "Gregorian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo UnknownProvider = new("xx-XX", "Unknown", "Unknown", false, "Segoe UI", NumberDigits.Latin, "Usd", "NotARealProvider", "1.0.0", "1.0", true);

    private static CalendarService CreateSut(LanguageInfo currentLanguage) =>
        new(
            new StubLocalizationService([Persian, English], currentLanguage),
            [new GregorianCalendarProvider(), new PersianCalendarProvider()]);

    [Fact]
    public void ToDisplayString_CurrentLanguageIsPersian_UsesPersianCalendarProvider()
    {
        var sut = CreateSut(Persian);

        // 2026-03-21 (Gregorian) is Farvardin 1, 1405 on the Persian calendar - Nowruz, same fixture DateProviderTests already uses.
        var result = sut.ToDisplayString(new DateTime(2026, 3, 21));

        Assert.Equal("1405/01/01", result);
    }

    [Fact]
    public void ToDisplayString_CurrentLanguageIsGregorian_UsesGregorianCalendarProvider()
    {
        var sut = CreateSut(English);

        var result = sut.ToDisplayString(new DateTime(2026, 3, 21));

        Assert.Equal("2026-03-21", result);
    }

    [Fact]
    public void ToDisplayString_CurrentLanguageNamesAnUnregisteredProvider_FallsBackToGregorian()
    {
        var sut = CreateSut(UnknownProvider);

        var result = sut.ToDisplayString(new DateTime(2026, 3, 21));

        Assert.Equal("2026-03-21", result);
    }

    [Fact]
    public void CurrentCulture_ReturnsThreadCurrentCulture()
    {
        var sut = CreateSut(English);

        Assert.Equal(CultureInfo.CurrentCulture, sut.CurrentCulture);
    }

    [Fact]
    public void Today_MatchesActiveProviderToday()
    {
        var sut = CreateSut(English);

        Assert.Equal(DateTime.Now.Date, sut.Today.Date);
    }
}
