using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>Default <see cref="IApprovalRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\automation\approvals.json</c>.</summary>
public sealed class LocalApprovalRepository : IApprovalRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalApprovalRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalApprovalRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApprovalRequest>>(ReadPersisted());

    public Task<ApprovalRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadPersisted().FirstOrDefault(request => request.Id == id));

    public Task SaveAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var requests = ReadPersisted();
        var index = requests.FindIndex(existing => existing.Id == request.Id);
        if (index >= 0)
        {
            requests[index] = request;
        }
        else
        {
            requests.Add(request);
        }

        Persist(requests);
        return Task.CompletedTask;
    }

    private List<ApprovalRequest> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ApprovalRequest>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<ApprovalRequest> requests)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(requests, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "approvals.json");
}
