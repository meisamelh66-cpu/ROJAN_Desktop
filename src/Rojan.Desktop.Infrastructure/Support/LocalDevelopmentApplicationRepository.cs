using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Infrastructure.Support;

/// <summary>Default <see cref="IDevelopmentApplicationRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\support\development-applications.json</c> - architecture only, per this sprint's own "Architecture only. Backend-ready." instruction.</summary>
public sealed class LocalDevelopmentApplicationRepository : IDevelopmentApplicationRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalDevelopmentApplicationRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalDevelopmentApplicationRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<DevelopmentApplication>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DevelopmentApplication>>(ReadPersisted());

    public Task SaveAsync(DevelopmentApplication application, CancellationToken cancellationToken = default)
    {
        var applications = ReadPersisted();
        applications.Add(application);
        Persist(applications);
        return Task.CompletedTask;
    }

    private List<DevelopmentApplication> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<DevelopmentApplication>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<DevelopmentApplication> applications)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(applications, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "support", "development-applications.json");
}
