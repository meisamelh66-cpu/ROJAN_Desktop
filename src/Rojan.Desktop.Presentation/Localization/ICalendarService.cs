using System.Globalization;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// Product requirement: the screens-only half of the centralized DateTime
/// service - "current culture" and "calendar conversion" are display
/// concerns Application/Infrastructure never need (see
/// <c>Application.Common.IDateTimeService</c>'s own doc comment for the
/// system-time/time-zone half, and for why the two are separate
/// interfaces rather than one spanning layers that cannot depend on each
/// other). Finally wires up <see cref="IDateProvider"/>, which has existed
/// since Phase 19A but - per its own doc comment - was never actually
/// consumed by anything: only <see cref="GregorianCalendarProvider"/> was
/// even registered in DI, so <see cref="PersianCalendarProvider"/> was
/// unreachable regardless of the active language. This service is the
/// first real consumer, selecting between every registered
/// <see cref="IDateProvider"/> by <see cref="LanguageInfo.DateProviderId"/>.
/// </summary>
public interface ICalendarService
{
    /// <summary>The culture active for this running session - the one sanctioned place to read it; existing ad-hoc <see cref="CultureInfo.CurrentCulture"/> reads elsewhere are a follow-up migration, not addressed by this foundation commit.</summary>
    public CultureInfo CurrentCulture { get; }

    /// <summary>Today's date in the active calendar (Gregorian/Persian, per <see cref="ILocalizationService.CurrentLanguage"/>).</summary>
    public DateTime Today { get; }

    /// <summary>Formats <paramref name="value"/> using the active calendar's own display convention (see the concrete <see cref="IDateProvider"/> implementations for the exact format).</summary>
    public string ToDisplayString(DateTime value);
}
