using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Infrastructure.Search;

/// <summary>Default <see cref="ISearchFavoritesStore"/>. Persists to <c>%LocalAppData%\RojanDesktop\search\favorites.json</c> - same "one concern, one file" shape every other persisted service in this app uses.</summary>
public sealed class LocalSearchFavoritesStore : ISearchFavoritesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalSearchFavoritesStore()
        : this(DefaultFilePath())
    {
    }

    internal LocalSearchFavoritesStore(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlySet<string>> GetFavoriteIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(ReadPersisted());

    public Task<bool> ToggleFavoriteAsync(string candidateId, CancellationToken cancellationToken = default)
    {
        var favorites = ReadPersisted();
        var isNowFavorite = favorites.Remove(candidateId) is false;
        if (isNowFavorite)
        {
            favorites.Add(candidateId);
        }

        Persist(favorites);
        return Task.FromResult(isNowFavorite);
    }

    private HashSet<string> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<HashSet<string>>(json, SerializerOptions) ?? [];
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

    private void Persist(HashSet<string> favorites)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(favorites, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "search", "favorites.json");
}
