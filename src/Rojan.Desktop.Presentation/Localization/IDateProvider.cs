namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// A pluggable calendar system for displaying dates - "future
/// extensibility only" per Phase 19A's scope: no existing View is wired
/// to one of these yet (every current date display still uses the
/// standard Gregorian <c>StringFormat</c> bindings established in every
/// prior phase), but the seam exists so a future phase can offer a
/// Persian-calendar date display without an architectural change,
/// matching <see cref="LanguageInfo.DateProviderId"/> per language.
/// </summary>
public interface IDateProvider
{
    /// <summary>Matches <see cref="LanguageInfo.DateProviderId"/> - e.g. <c>"Persian"</c>/<c>"Gregorian"</c>.</summary>
    public string ProviderId { get; }

    public string ToDisplayString(DateTime value);

    public DateTime Today { get; }
}
