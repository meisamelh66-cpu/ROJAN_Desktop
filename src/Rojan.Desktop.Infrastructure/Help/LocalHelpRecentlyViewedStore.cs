using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Infrastructure.Help;

/// <summary>
/// Default <see cref="IHelpRecentlyViewedStore"/>. Persists to
/// <c>%LocalAppData%\RojanDesktop\help\recent.json</c>, most-recent-first,
/// capped at <see cref="MaxEntries"/> - the actual recency cache Phase
/// 26.12 asks for ("cache recently used topics"), not merely a display
/// list: capping the persisted set is itself the eviction policy, so
/// nothing unbounded accumulates across a long-running session.
/// </summary>
public sealed class LocalHelpRecentlyViewedStore : IHelpRecentlyViewedStore
{
    public const int MaxEntries = 10;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalHelpRecentlyViewedStore()
        : this(DefaultFilePath())
    {
    }

    internal LocalHelpRecentlyViewedStore(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<string>> GetRecentTopicIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(ReadPersisted());

    public Task RecordViewedAsync(string topicId, CancellationToken cancellationToken = default)
    {
        var recent = ReadPersisted();
        recent.Remove(topicId);
        recent.Insert(0, topicId);
        if (recent.Count > MaxEntries)
        {
            recent.RemoveRange(MaxEntries, recent.Count - MaxEntries);
        }

        Persist(recent);
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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "help", "recent.json");
}
