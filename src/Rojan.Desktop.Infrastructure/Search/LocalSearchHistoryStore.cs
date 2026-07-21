using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Infrastructure.Search;

/// <summary>Default <see cref="ISearchHistoryStore"/>. Persists to <c>%LocalAppData%\RojanDesktop\search\history.json</c> - same "one concern, one file" shape every other persisted service in this app uses.</summary>
public sealed class LocalSearchHistoryStore : ISearchHistoryStore
{
    public const int MaxEntries = 10;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalSearchHistoryStore()
        : this(DefaultFilePath())
    {
    }

    internal LocalSearchHistoryStore(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<string>> GetRecentSearchesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(ReadPersisted());

    public Task RecordSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0)
        {
            return Task.CompletedTask;
        }

        var recent = ReadPersisted();
        recent.RemoveAll(q => string.Equals(q, trimmed, StringComparison.OrdinalIgnoreCase));
        recent.Insert(0, trimmed);
        if (recent.Count > MaxEntries)
        {
            recent.RemoveRange(MaxEntries, recent.Count - MaxEntries);
        }

        Persist(recent);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Persist([]);
        return Task.CompletedTask;
    }

    private List<string> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<string>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Persist(List<string> recent)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(recent, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "search", "history.json");
}
