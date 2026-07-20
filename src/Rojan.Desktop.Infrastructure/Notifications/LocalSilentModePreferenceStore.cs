using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Infrastructure.Notifications;

/// <summary>Default <see cref="ISilentModePreferenceStore"/>. Persists to <c>%LocalAppData%\RojanDesktop\notifications\silent-mode.json</c> - same "one concern, one file" shape every other persisted service in this app uses.</summary>
public sealed class LocalSilentModePreferenceStore : ISilentModePreferenceStore
{
    private sealed record SilentModePreference(bool IsEnabled);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalSilentModePreferenceStore()
        : this(DefaultFilePath())
    {
    }

    internal LocalSilentModePreferenceStore(string filePath)
    {
        _filePath = filePath;
    }

    public Task<bool> GetIsEnabledAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var preference = JsonSerializer.Deserialize<SilentModePreference>(json, SerializerOptions);
            return Task.FromResult(preference?.IsEnabled ?? false);
        }
        catch (JsonException)
        {
            return Task.FromResult(false);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
    }

    public Task SetIsEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(new SilentModePreference(isEnabled), SerializerOptions));
        return Task.CompletedTask;
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "notifications", "silent-mode.json");
}
