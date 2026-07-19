namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// Discovers every installable language - the three built-in ones
/// (compiled satellite resources) plus every <c>Languages/*.pack</c>
/// manifest found on disk. This is the seam that makes "unlimited
/// languages without changing application code" real: dropping a new
/// <c>.pack</c> file into the <c>Languages/</c> folder makes it appear in
/// <see cref="ILocalizationService.AvailableLanguages"/> on next launch,
/// with zero source changes.
/// </summary>
public interface ILanguagePackManager
{
    public Task<IReadOnlyList<LanguageInfo>> DiscoverLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>The string-override table a pack supplies for <paramref name="languageCode"/>, if any - "Future packs load their own resources" made concrete. Null when the pack has no override table (built-in languages resolve entirely through compiled satellite resources instead).</summary>
    public Task<IReadOnlyDictionary<string, string>?> GetPackStringOverridesAsync(string languageCode, CancellationToken cancellationToken = default);
}
