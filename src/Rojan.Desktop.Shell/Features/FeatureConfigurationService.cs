using System.IO;
using System.Text.Json;
using Rojan.Desktop.Presentation.Features;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Shell.Features;

/// <summary>
/// Default <see cref="IFeatureConfigurationService"/>. Persists to
/// <c>%LocalAppData%\RojanDesktop\features\feature-config.json</c> - one file, keyed internally by
/// salon id (see <see cref="FeatureConfigFile"/>), same "one concern, one file" shape
/// <c>Shell.Organizations.CurrentSessionService</c>'s own <c>session.json</c> already establishes,
/// just with an extra per-salon dimension inside it (see this class's own <c>_scopeKey</c>).
///
/// Depends on <see cref="ICurrentSessionService"/> only to read the already-resolved
/// <see cref="ICurrentSessionService.CurrentOrganization"/> id at <see cref="InitializeAsync"/> time -
/// never subscribes to <see cref="ICurrentSessionService.SessionChanged"/>, since a real session's
/// active salon cannot change without a fresh app launch (see that interface's own
/// <c>SwitchBranchAsync</c> guard against crossing salons) - the scope is resolved once and stays
/// fixed for this instance's lifetime, same singleton lifetime as <c>CurrentSessionService</c> itself.
/// </summary>
public sealed class FeatureConfigurationService : IFeatureConfigurationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>The bucket used when no real salon is resolved yet (demo mode, or a brand-new invitee with no membership) - there is no real salon identity to key by, so every such session shares this one fixed key rather than each silently getting its own throwaway bucket.</summary>
    private const string NoSalonScopeKey = "__no_salon__";

    private readonly ICurrentSessionService _currentSessionService;
    private readonly string _settingsFilePath;
    private string _scopeKey = NoSalonScopeKey;
    private HashSet<string> _enabledFeatureKeys = [];
    private bool _hasConfiguration;

    public FeatureConfigurationService(ICurrentSessionService currentSessionService)
        : this(currentSessionService, DefaultSettingsFilePath())
    {
    }

    /// <summary>Test-only seam (see <c>Rojan.Desktop.Shell.csproj</c>'s <c>InternalsVisibleTo</c>) - lets tests point persistence at a throwaway path instead of the real <c>%LocalAppData%</c>, same pattern <c>Infrastructure.Salons.BackendSalonContextService</c> already establishes.</summary>
    internal FeatureConfigurationService(ICurrentSessionService currentSessionService, string settingsFilePath)
    {
        _currentSessionService = currentSessionService;
        _settingsFilePath = settingsFilePath;
    }

    public bool HasConfiguration => _hasConfiguration;

    public IReadOnlySet<string> EnabledFeatureKeys => _enabledFeatureKeys;

    public event EventHandler? ConfigurationChanged;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _scopeKey = _currentSessionService.CurrentOrganization?.Id is { Length: > 0 } salonId ? salonId : NoSalonScopeKey;

        var file = ReadFile();
        if (file.BySalonId.TryGetValue(_scopeKey, out var savedKeys))
        {
            _enabledFeatureKeys = savedKeys.ToHashSet();
            _hasConfiguration = true;
        }
        else
        {
            _enabledFeatureKeys = [];
            _hasConfiguration = false;
        }

        return Task.CompletedTask;
    }

    public bool IsFeatureEnabled(string featureKey) => _enabledFeatureKeys.Contains(featureKey);

    public Task SaveEnabledFeatureKeysAsync(IReadOnlySet<string> enabledFeatureKeys, CancellationToken cancellationToken = default)
    {
        var file = ReadFile();
        file.BySalonId[_scopeKey] = enabledFeatureKeys.ToList();
        PersistFile(file);

        _enabledFeatureKeys = enabledFeatureKeys.ToHashSet();
        _hasConfiguration = true;

        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private FeatureConfigFile ReadFile()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new FeatureConfigFile();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<FeatureConfigFile>(json, SerializerOptions) ?? new FeatureConfigFile();
        }
        // A corrupt or unreadable settings file must fall back to "never configured," never crash
        // startup or silently keep a stale in-memory guess - see IFeatureConfigurationService's own
        // doc comment on InitializeAsync.
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return new FeatureConfigFile();
        }
    }

    private void PersistFile(FeatureConfigFile file)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(file, SerializerOptions));
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "features", "feature-config.json");

    /// <summary>The whole persisted file - every salon's saved selection in one place, keyed by salon id (or <see cref="NoSalonScopeKey"/>). Mutable, JSON-friendly shape (not a positional record) - same convention <c>Infrastructure.Salons.BackendSalonContextService.ActiveSalonSettingsFile</c>/<c>Infrastructure.Workspaces.LocalWorkspaceStore</c>'s own record types already use for anything <see cref="JsonSerializer"/> deserializes.</summary>
    private sealed class FeatureConfigFile
    {
        public Dictionary<string, List<string>> BySalonId { get; set; } = [];
    }
}
