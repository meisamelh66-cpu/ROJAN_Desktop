namespace Rojan.Desktop.Application.Api;

/// <summary>
/// Owner App Login Experience: resolves which backend base address
/// <see cref="IApiClient"/>/<c>AuthBootstrapHttpClient</c> point at.
/// Resolution order (highest priority first):
/// <list type="number">
/// <item><c>ROJAN_API_BASE_URL</c> environment variable - unchanged from
/// before this feature, so an ops/CI override still always wins.</item>
/// <item>The persisted <see cref="SelectedEnvironment"/> choice's
/// configured URL (Development defaults to <c>http://localhost:8080</c>,
/// matching ROJAN_Backend's own dev-profile default port; Production
/// falls back to <c>Infrastructure.Api.ApiEnvironmentService.ProductionUrlDefault</c>
/// - Desktop Productionization Sprint 1: this is the one real,
/// already-documented production domain (matches
/// <c>Support.RojanBrandConfiguration.ApiBaseUrl</c> and ROJAN_Backend's
/// own prod compose file), not a guess, so baking it in doesn't reintroduce
/// the "silently send real credentials to a guessed domain" risk this
/// resolution order was originally written to avoid - an explicit
/// <see cref="SetEnvironmentAsync"/> override still always wins over it).</item>
/// </list>
/// Same "persist + flag IsRestartRequired, never apply live" shape
/// <c>Shell.Theming.IThemeService</c>/<c>ILocalizationService</c> already
/// establish - <see cref="IApiClient"/>'s underlying <see cref="HttpClient"/>
/// base address is set once at construction, so a change here needs a
/// relaunch to take effect, exactly like a theme/language change does.
/// </summary>
public interface IApiEnvironmentService
{
    public ApiEnvironment SelectedEnvironment { get; }

    /// <summary>The effective base address after applying the resolution order above. Never <see langword="null"/> for Production since Desktop Productionization Sprint 1 (falls back to the real production default); still <see langword="null"/> for Development only in the theoretical case <see cref="ApiEnvironment.Development"/>'s own hardcoded URL is somehow blank, which cannot happen today.</summary>
    public Uri? ResolvedBaseAddress { get; }

    public string? ProductionUrl { get; }

    public bool IsRestartRequired { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary><paramref name="productionUrl"/> is only stored/used when <paramref name="environment"/> is <see cref="ApiEnvironment.Production"/> or was previously set - passing <see langword="null"/> leaves a previously-configured Production URL unchanged.</summary>
    public Task SetEnvironmentAsync(ApiEnvironment environment, string? productionUrl, CancellationToken cancellationToken = default);
}
