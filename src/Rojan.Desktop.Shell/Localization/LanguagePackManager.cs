using System.IO;
using System.Text.Json;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Shell.Localization;

/// <summary>
/// Default <see cref="ILanguagePackManager"/> implementation - scans the
/// <c>Languages/</c> folder next to the executable for <c>*.pack</c>
/// files (JSON manifests, see <see cref="PackManifest"/>) and parses each
/// into a <see cref="LanguageInfo"/>. This is the concrete proof of "the
/// application must automatically detect installed packs": every
/// built-in language (fa-IR/en-US/ar-SA) ships as a real pack file here
/// too, not a hardcoded list - dropping a new <c>.pack</c> file in is the
/// entire "install a language" story for this foundation phase (no
/// application code changes), and a malformed/unreadable pack is skipped
/// rather than failing startup.
/// </summary>
public sealed class LanguagePackManager : ILanguagePackManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _languagesDirectory;

    public LanguagePackManager()
        : this(Path.Combine(AppContext.BaseDirectory, "Languages"))
    {
    }

    internal LanguagePackManager(string languagesDirectory)
    {
        _languagesDirectory = languagesDirectory;
    }

    public Task<IReadOnlyList<LanguageInfo>> DiscoverLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var languages = new List<LanguageInfo>();

        if (Directory.Exists(_languagesDirectory))
        {
            foreach (var packPath in Directory.EnumerateFiles(_languagesDirectory, "*.pack"))
            {
                var manifest = TryReadManifest(packPath);
                var language = ToLanguageInfo(manifest);
                if (language is not null)
                {
                    languages.Add(language);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<LanguageInfo>>(languages);
    }

    public Task<IReadOnlyDictionary<string, string>?> GetPackStringOverridesAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_languagesDirectory))
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
        }

        var packPath = Path.Combine(_languagesDirectory, $"{languageCode}.pack");
        if (!File.Exists(packPath))
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
        }

        var manifest = TryReadManifest(packPath);
        return Task.FromResult<IReadOnlyDictionary<string, string>?>(manifest?.Strings);
    }

    private static PackManifest? TryReadManifest(string packPath)
    {
        try
        {
            var json = File.ReadAllText(packPath);
            return JsonSerializer.Deserialize<PackManifest>(json, SerializerOptions);
        }
#pragma warning disable CA1031 // A corrupt/unreadable pack file must be skipped, not crash pack discovery or startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private static LanguageInfo? ToLanguageInfo(PackManifest? manifest)
    {
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Code))
        {
            return null;
        }

        if (!Enum.TryParse<NumberDigits>(manifest.NumberDigits, ignoreCase: true, out var numberDigits))
        {
            numberDigits = NumberDigits.Latin;
        }

        return new LanguageInfo(
            manifest.Code,
            manifest.NativeName,
            manifest.EnglishName,
            manifest.IsRightToLeft,
            manifest.FontFamily,
            numberDigits,
            manifest.DefaultCurrencyCode,
            manifest.DateProviderId,
            manifest.PackVersion,
            manifest.CompatibilityVersion,
            manifest.IsBuiltIn);
    }
}
