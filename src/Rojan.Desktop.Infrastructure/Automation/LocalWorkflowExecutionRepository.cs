using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>
/// Default <see cref="IWorkflowExecutionRepository"/>. Persists to
/// <c>%LocalAppData%\RojanDesktop\automation\executions.json</c>. Capped
/// at <see cref="MaxEntries"/>, oldest-first eviction - Requirement 32.8's
/// Execution History is a bounded window like Phase 27's notification
/// history, not an unbounded audit log, so a long-running session never
/// accumulates disk/memory without limit.
/// </summary>
public sealed class LocalWorkflowExecutionRepository : IWorkflowExecutionRepository
{
    public const int MaxEntries = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalWorkflowExecutionRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalWorkflowExecutionRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<WorkflowExecution>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowExecution>>(ReadPersisted());

    public Task<WorkflowExecution?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadPersisted().FirstOrDefault(execution => execution.Id == id));

    public Task SaveAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        var executions = ReadPersisted();
        var index = executions.FindIndex(existing => existing.Id == execution.Id);
        if (index >= 0)
        {
            executions[index] = execution;
        }
        else
        {
            executions.Insert(0, execution);
            if (executions.Count > MaxEntries)
            {
                executions.RemoveRange(MaxEntries, executions.Count - MaxEntries);
            }
        }

        Persist(executions);
        return Task.CompletedTask;
    }

    private List<WorkflowExecution> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<WorkflowExecution>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<WorkflowExecution> executions)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(executions, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "executions.json");
}
