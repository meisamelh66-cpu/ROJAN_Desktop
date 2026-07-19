namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Standard Gregorian calendar date display - see <see cref="IDateProvider"/>'s own "future extensibility only" note.</summary>
public sealed class GregorianCalendarProvider : IDateProvider
{
    public string ProviderId => "Gregorian";

    public DateTime Today => DateTime.Now;

    public string ToDisplayString(DateTime value) => value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
