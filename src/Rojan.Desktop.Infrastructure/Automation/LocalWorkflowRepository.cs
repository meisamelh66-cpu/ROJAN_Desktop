using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>Default <see cref="IWorkflowRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\automation\workflows.json</c> - same "one concern, one file" shape every other persisted service in this app uses. Every version of every workflow lineage is stored as its own record (see <see cref="WorkflowDefinition.ParentWorkflowId"/>).</summary>
public sealed class LocalWorkflowRepository : IWorkflowRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalWorkflowRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalWorkflowRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinition>>(ReadPersisted());

    public Task<WorkflowDefinition?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadPersisted().FirstOrDefault(workflow => workflow.Id == id));

    public Task<IReadOnlyList<WorkflowDefinition>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinition>>(
            ReadPersisted().Where(workflow => workflow.ParentWorkflowId == parentWorkflowId).OrderByDescending(workflow => workflow.Version).ToList());

    public Task SaveAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var workflows = ReadPersisted();
        var index = workflows.FindIndex(existing => existing.Id == workflow.Id);
        if (index >= 0)
        {
            workflows[index] = workflow;
        }
        else
        {
            workflows.Add(workflow);
        }

        Persist(workflows);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var workflows = ReadPersisted();
        if (workflows.RemoveAll(workflow => workflow.Id == id) > 0)
        {
            Persist(workflows);
        }

        return Task.CompletedTask;
    }

    private List<WorkflowDefinition> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<WorkflowDefinition>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<WorkflowDefinition> workflows)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(workflows, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "workflows.json");
}
