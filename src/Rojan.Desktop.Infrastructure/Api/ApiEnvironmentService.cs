using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Api;

namespace Rojan.Desktop.Infrastructure.Api;

/// <summary>
/// Default <see cref="IApiEnvironmentService"/>. Persists the selected
/// <see cref="ApiEnvironment"/> (and, if set, a custom Production URL) to
/// its own small JSON file under LocalAppData - same "one concern, one
/// file" shape <c>Shell.Theming.ThemeService</c> already establishes.
/// Development is always the first-launch default (no settings file yet
/// -&gt; Development), so a fresh install never accidentally points at a
/// misconfigured or unset Production address.
///
/// Desktop Productionization Sprint 1: <see cref="ProductionUrl"/> now
/// falls back to <see cref="ProductionUrlDefault"/> - matching
/// <c>Support.RojanBrandConfiguration.ApiBaseUrl</c>'s already-documented
/// intended value and ROJAN_Backend's own prod compose file
/// (<c>MEDIA_PUBLIC_BASE_URL</c>/<c>ROJAN_PUBLIC_BASE_URL</c> both resolve
/// under the same <c>rojanai.ir</c> domain in
/// <c>docker-compose.prod.yml</c>) - instead of resolving to <c>null</c>
/// until an operator manually sets it via Settings. Switching to
/// Production is still an explicit user action (Development stays the
/// first-launch default per the doc comment above); this only fixes what
/// that switch resolves to on a fresh install with no persisted override.
/// </summary>
public sealed class ApiEnvironmentService : IApiEnvironmentService
{
    private const string EnvironmentVariable = "ROJAN_API_BASE_URL";
    private const string DevelopmentUrl = "http://localhost:8080";

    /// <summary>The real production API address, baked in so a fresh install's Production toggle resolves to something reachable without a manual Settings step. A user-supplied override (via <see cref="SetEnvironmentAsync"/> or the <see cref="EnvironmentVariable"/> env var) always takes precedence - this is a fallback, not a hardcoded lock-in.</summary>
    public const string ProductionUrlDefault = "https://api.rojanai.ir";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _settingsFilePath;

    public ApiEnvironmentService()
        : this(DefaultSettingsFilePath())
    {
    }

    internal ApiEnvironmentService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public ApiEnvironment SelectedEnvironment { get; private set; } = ApiEnvironment.Development;

    public string? ProductionUrl { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public Uri? ResolvedBaseAddress
    {
        get
        {
            var overrideAddress = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideAddress))
            {
                return new Uri(overrideAddress, UriKind.Absolute);
            }

            var configuredUrl = SelectedEnvironment == ApiEnvironment.Development
                ? DevelopmentUrl
                : (string.IsNullOrWhiteSpace(ProductionUrl) ? ProductionUrlDefault : ProductionUrl);
            return string.IsNullOrWhiteSpace(configuredUrl) ? null : new Uri(configuredUrl, UriKind.Absolute);
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var persisted = ReadPersisted();
        if (persisted is not null)
        {
            SelectedEnvironment = Enum.TryParse<ApiEnvironment>(persisted.Environment, ignoreCase: true, out var environment)
                ? environment
                : ApiEnvironment.Development;
            ProductionUrl = persisted.ProductionUrl;
        }

        return Task.CompletedTask;
    }

    public Task SetEnvironmentAsync(ApiEnvironment environment, string? productionUrl, CancellationToken cancellationToken = default)
    {
        var effectiveProductionUrl = productionUrl ?? ProductionUrl;
        var addressChanged = environment != SelectedEnvironment || (environment == ApiEnvironment.Production && productionUrl is not null && productionUrl != ProductionUrl);

        Persist(environment, effectiveProductionUrl);
        SelectedEnvironment = environment;
        ProductionUrl = effectiveProductionUrl;

        if (addressChanged)
        {
            IsRestartRequired = true;
        }

        return Task.CompletedTask;
    }

    private ApiEnvironmentSettingsFile? ReadPersisted()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<ApiEnvironmentSettingsFile>(json, SerializerOptions);
        }
#pragma warning disable CA1031 // A corrupt settings file must fall back to the default environment, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void Persist(ApiEnvironment environment, string? productionUrl)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new ApiEnvironmentSettingsFile { Environment = environment.ToString(), ProductionUrl = productionUrl }, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "api", "environment.json");

    private sealed class ApiEnvironmentSettingsFile
    {
        public string Environment { get; set; } = string.Empty;

        public string? ProductionUrl { get; set; }
    }
}
