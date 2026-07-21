using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>Default <see cref="IScheduledJobRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\automation\jobs.json</c>.</summary>
public sealed class LocalScheduledJobRepository : IScheduledJobRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalScheduledJobRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalScheduledJobRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<ScheduledJob>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledJob>>(ReadPersisted());

    public Task<ScheduledJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadPersisted().FirstOrDefault(job => job.Id == id));

    public Task SaveAsync(ScheduledJob job, CancellationToken cancellationToken = default)
    {
        var jobs = ReadPersisted();
        var index = jobs.FindIndex(existing => existing.Id == job.Id);
        if (index >= 0)
        {
            jobs[index] = job;
        }
        else
        {
            jobs.Add(job);
        }

        Persist(jobs);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var jobs = ReadPersisted();
        if (jobs.RemoveAll(job => job.Id == id) > 0)
        {
            Persist(jobs);
        }

        return Task.CompletedTask;
    }

    private List<ScheduledJob> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ScheduledJob>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<ScheduledJob> jobs)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(jobs, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "jobs.json");
}
