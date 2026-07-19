using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Localization;

public sealed class DateProviderTests
{
    [Fact]
    public void GregorianCalendarProvider_ToDisplayString_UsesIsoOrder()
    {
        var provider = new GregorianCalendarProvider();

        var result = provider.ToDisplayString(new DateTime(2026, 3, 21));

        Assert.Equal("2026-03-21", result);
        Assert.Equal("Gregorian", provider.ProviderId);
    }

    [Fact]
    public void PersianCalendarProvider_ToDisplayString_ConvertsToJalaliCalendar()
    {
        var provider = new PersianCalendarProvider();

        // 2026-03-21 (Gregorian) is Farvardin 1, 1405 on the Persian calendar - Nowruz.
        var result = provider.ToDisplayString(new DateTime(2026, 3, 21));

        Assert.Equal("1405/01/01", result);
        Assert.Equal("Persian", provider.ProviderId);
    }
}
