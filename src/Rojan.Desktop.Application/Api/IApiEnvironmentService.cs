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
/// matching ROJAN_Backend's own dev-profile default port; Production has
/// no built-in default - it must be set explicitly via
/// <see cref="SetEnvironmentAsync"/> before selecting it resolves to
/// anything, so the app can never silently send real credentials to a
/// guessed domain).</item>
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

    /// <summary>The effective base address after applying the resolution order above - <see langword="null"/> only if Production is selected but no URL has ever been configured for it.</summary>
    public Uri? ResolvedBaseAddress { get; }

    public string? ProductionUrl { get; }

    public bool IsRestartRequired { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary><paramref name="productionUrl"/> is only stored/used when <paramref name="environment"/> is <see cref="ApiEnvironment.Production"/> or was previously set - passing <see langword="null"/> leaves a previously-configured Production URL unchanged.</summary>
    public Task SetEnvironmentAsync(ApiEnvironment environment, string? productionUrl, CancellationToken cancellationToken = default);
}
