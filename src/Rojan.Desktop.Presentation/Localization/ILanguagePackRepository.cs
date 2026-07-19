namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// The "future online download" seam Phase 19A's "LANGUAGE STORE
/// FOUNDATION" requirement asks for - architecture only. Per that
/// requirement's explicit instruction ("Do NOT connect to servers yet.
/// Only build the framework"), <c>Shell.Localization.LocalOnlyLanguagePackRepository</c>
/// is the only implementation this phase ships, and it never makes a
/// network call - a real HTTP-backed implementation is later work,
/// swapped in behind this same interface with no caller change (same
/// "swap the implementation, not the interface" pattern every other
/// module's fake-to-real repository story in this app follows).
/// </summary>
public interface ILanguagePackRepository
{
    public Task<IReadOnlyList<LanguagePackCatalogEntry>> GetAvailableLanguagePacksAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads and installs the given language pack, returning the installed <see cref="LanguageInfo"/>. Not implemented by <c>LocalOnlyLanguagePackRepository</c> - throws <see cref="NotSupportedException"/> until a real online store exists.</summary>
    public Task<LanguageInfo> DownloadAndInstallAsync(string languageCode, CancellationToken cancellationToken = default);

    /// <summary>Removes an installed (non-built-in) language pack.</summary>
    public Task RemovePackAsync(string languageCode, CancellationToken cancellationToken = default);
}
