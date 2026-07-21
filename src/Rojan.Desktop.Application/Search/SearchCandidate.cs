namespace Rojan.Desktop.Application.Search;

/// <summary>
/// Phase 28: Enterprise Global Search &amp; Command Palette. One
/// searchable/executable entry, already carrying plain display text -
/// live business data (Customer/Booking/Specialist/Service/Product) is
/// already plain text at its own DTO layer (a customer's name needs no
/// localization), while static entries (Page/Command) are built by
/// Presentation with already-resolved localized text
/// (<c>Presentation.Search.StaticSearchCatalog</c>), the same "only
/// Presentation can see <c>Strings</c>" reasoning behind every other
/// module's *SearchCandidate shape in this codebase.
/// </summary>
/// <param name="Id">Stable identifier, unique within <paramref name="Type"/>.</param>
/// <param name="Type">Which kind of result this is - drives the icon and type-priority ranking boost.</param>
/// <param name="Title">The primary matched/displayed text.</param>
/// <param name="Subtitle">Secondary display text (e.g. a customer's company, a booking's time) - never matched against, display only.</param>
/// <param name="Keywords">Additional matchable-but-not-displayed terms (e.g. a product's SKU, a command's alternate phrasing) - boosts recall without cluttering the title.</param>
/// <param name="ActionKey">What selecting this result does - <c>"page:{moduleId}"</c> navigates, <c>"command:{commandId}"</c> executes a registered command. Interpreted only by Presentation/Shell.</param>
public sealed record SearchCandidate(
    string Id,
    SearchResultType Type,
    string Title,
    string Subtitle,
    IReadOnlyList<string> Keywords,
    string ActionKey);
