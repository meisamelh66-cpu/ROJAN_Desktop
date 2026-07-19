namespace Rojan.Desktop.Presentation.Localization;

/// <summary>One entry in a remote language-pack catalog - the "Available Languages" list Settings would show once a real store exists (see <see cref="ILanguagePackRepository"/>).</summary>
public sealed record LanguagePackCatalogEntry(
    string Code,
    string NativeName,
    string EnglishName,
    string LatestVersion,
    bool IsInstalled);
