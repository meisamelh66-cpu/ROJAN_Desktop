using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Shell.Localization;

/// <summary>
/// The only <see cref="ILanguagePackRepository"/> implementation Phase
/// 19A ships - it never makes a network call, per that phase's explicit
/// "Do NOT connect to servers yet. Only build the framework" instruction.
/// The catalog is always empty (nothing to download yet); install/remove
/// both signal "not supported yet" rather than silently no-op, so a
/// future real implementation's absence is never mistaken for success.
/// </summary>
public sealed class LocalOnlyLanguagePackRepository : ILanguagePackRepository
{
    public Task<IReadOnlyList<LanguagePackCatalogEntry>> GetAvailableLanguagePacksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LanguagePackCatalogEntry>>([]);

    public Task<LanguageInfo> DownloadAndInstallAsync(string languageCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Online language pack downloads are not available yet - Phase 19A ships the framework only.");

    public Task RemovePackAsync(string languageCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Language pack removal is not available yet - Phase 19A ships the framework only.");
}
