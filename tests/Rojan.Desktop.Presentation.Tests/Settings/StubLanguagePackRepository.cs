using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Settings;

/// <summary>Fakes <see cref="ILanguagePackRepository"/> for SettingsPageViewModel tests, standing in for Shell.Localization.LocalOnlyLanguagePackRepository's always-empty/NotSupportedException behavior with a configurable catalog.</summary>
internal sealed class StubLanguagePackRepository : ILanguagePackRepository
{
    private readonly List<LanguagePackCatalogEntry> _catalog;

    public StubLanguagePackRepository(IReadOnlyList<LanguagePackCatalogEntry>? catalog = null)
    {
        _catalog = catalog?.ToList() ?? [];
    }

    public Task<IReadOnlyList<LanguagePackCatalogEntry>> GetAvailableLanguagePacksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LanguagePackCatalogEntry>>(_catalog);

    public Task<LanguageInfo> DownloadAndInstallAsync(string languageCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Online language pack downloads are not available yet - Phase 19A ships the framework only.");

    public Task RemovePackAsync(string languageCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Language pack removal is not available yet - Phase 19A ships the framework only.");
}
