using System.Globalization;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Persian (Jalali) calendar date display - see <see cref="IDateProvider"/>'s own "future extensibility only" note.</summary>
public sealed class PersianCalendarProvider : IDateProvider
{
    private static readonly PersianCalendar Calendar = new();

    public string ProviderId => "Persian";

    public DateTime Today => DateTime.Now;

    public string ToDisplayString(DateTime value) =>
        $"{Calendar.GetYear(value):D4}/{Calendar.GetMonth(value):D2}/{Calendar.GetDayOfMonth(value):D2}";
}
