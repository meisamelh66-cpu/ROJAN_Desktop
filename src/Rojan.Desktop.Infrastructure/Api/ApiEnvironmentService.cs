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
/// </summary>
public sealed class ApiEnvironmentService : IApiEnvironmentService
{
    private const string EnvironmentVariable = "ROJAN_API_BASE_URL";
    private const string DevelopmentUrl = "http://localhost:8080";

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

            var configuredUrl = SelectedEnvironment == ApiEnvironment.Development ? DevelopmentUrl : ProductionUrl;
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
