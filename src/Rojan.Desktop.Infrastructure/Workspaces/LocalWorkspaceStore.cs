using System.IO;
using System.Text.Json;
using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Infrastructure.Workspaces;

/// <summary>
/// Default <see cref="DomainWorkspaces.IWorkspaceRepository"/>. Persists to
/// <c>%LocalAppData%\RojanDesktop\workspaces\workspaces.json</c> (the saved
/// layouts themselves) and <c>...\workspaces\state.json</c> (which one is
/// active plus the Recent Workspaces list, capped at
/// <see cref="MaxRecentEntries"/>) - two files because they're two
/// independently-changing concerns (editing a layout vs. switching which one
/// is active), the same granularity <c>LocalNotificationRepository</c>/
/// <c>LocalSilentModePreferenceStore</c> already split notification history
/// from its own preference.
/// </summary>
public sealed class LocalWorkspaceStore : DomainWorkspaces.IWorkspaceRepository
{
    public const int MaxRecentEntries = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _workspacesFilePath;
    private readonly string _stateFilePath;

    public LocalWorkspaceStore()
        : this(DefaultWorkspacesFilePath(), DefaultStateFilePath())
    {
    }

    internal LocalWorkspaceStore(string workspacesFilePath, string stateFilePath)
    {
        _workspacesFilePath = workspacesFilePath;
        _stateFilePath = stateFilePath;
    }

    public Task<IReadOnlyList<DomainWorkspaces.WorkspaceLayout>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainWorkspaces.WorkspaceLayout>>(ReadWorkspaces().Select(ToDomain).ToList());

    public Task<DomainWorkspaces.WorkspaceLayout?> GetByIdAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var record = ReadWorkspaces().FirstOrDefault(w => w.Id == workspaceId);
        return Task.FromResult(record is null ? null : ToDomain(record));
    }

    public Task SaveAsync(DomainWorkspaces.WorkspaceLayout layout, CancellationToken cancellationToken = default)
    {
        var workspaces = ReadWorkspaces();
        var index = workspaces.FindIndex(w => w.Id == layout.Id);
        var record = ToRecord(layout);
        if (index >= 0)
        {
            workspaces[index] = record;
        }
        else
        {
            workspaces.Add(record);
        }

        PersistWorkspaces(workspaces);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var workspaces = ReadWorkspaces();
        if (workspaces.RemoveAll(w => w.Id == workspaceId) > 0)
        {
            PersistWorkspaces(workspaces);
        }

        var state = ReadState();
        if (state.RecentWorkspaceIds.RemoveAll(id => id == workspaceId) > 0)
        {
            PersistState(state);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetActiveWorkspaceIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadState().ActiveWorkspaceId);

    public Task SetActiveWorkspaceIdAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        PersistState(ReadState() with { ActiveWorkspaceId = workspaceId });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetRecentWorkspaceIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(ReadState().RecentWorkspaceIds);

    public Task RecordRecentWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var state = ReadState();
        var recent = state.RecentWorkspaceIds.ToList();
        recent.RemoveAll(id => id == workspaceId);
        recent.Insert(0, workspaceId);
        if (recent.Count > MaxRecentEntries)
        {
            recent.RemoveRange(MaxRecentEntries, recent.Count - MaxRecentEntries);
        }

        PersistState(state with { RecentWorkspaceIds = recent });
        return Task.CompletedTask;
    }

    private List<WorkspaceLayoutRecord> ReadWorkspaces()
    {
        if (!File.Exists(_workspacesFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_workspacesFilePath);
            return JsonSerializer.Deserialize<List<WorkspaceLayoutRecord>>(json, SerializerOptions) ?? [];
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

    private void PersistWorkspaces(List<WorkspaceLayoutRecord> workspaces)
    {
        EnsureDirectory(_workspacesFilePath);
        File.WriteAllText(_workspacesFilePath, JsonSerializer.Serialize(workspaces, SerializerOptions));
    }

    private WorkspaceStateRecord ReadState()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new WorkspaceStateRecord(null, []);
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<WorkspaceStateRecord>(json, SerializerOptions) ?? new WorkspaceStateRecord(null, []);
        }
        catch (JsonException)
        {
            return new WorkspaceStateRecord(null, []);
        }
        catch (IOException)
        {
            return new WorkspaceStateRecord(null, []);
        }
    }

    private void PersistState(WorkspaceStateRecord state)
    {
        EnsureDirectory(_stateFilePath);
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state, SerializerOptions));
    }

    private static void EnsureDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static DomainWorkspaces.WorkspaceLayout ToDomain(WorkspaceLayoutRecord record) => new(
        record.Id,
        record.Name,
        record.PrimaryModuleId,
        ToDomain(record.SecondaryRoot),
        record.DockedPanels,
        record.FloatingWindows,
        record.CreatedAt,
        record.UpdatedAt,
        record.IsDefault);

    private static DomainWorkspaces.PaneNode? ToDomain(PaneNodeRecord? record) => record switch
    {
        null => null,
        PaneLeafRecord leaf => new DomainWorkspaces.PaneLeaf(leaf.Id, leaf.ModuleIds, leaf.ActiveModuleId),
        PaneSplitRecord split => new DomainWorkspaces.PaneSplit(split.Id, split.Orientation, split.Ratio, ToDomain(split.First)!, ToDomain(split.Second)!),
        _ => throw new InvalidOperationException($"Unknown pane node record type '{record.GetType()}'."),
    };

    private static WorkspaceLayoutRecord ToRecord(DomainWorkspaces.WorkspaceLayout layout) => new(
        layout.Id,
        layout.Name,
        layout.PrimaryModuleId,
        ToRecord(layout.SecondaryRoot),
        layout.DockedPanels,
        layout.FloatingWindows,
        layout.CreatedAt,
        layout.UpdatedAt,
        layout.IsDefault);

    private static PaneNodeRecord? ToRecord(DomainWorkspaces.PaneNode? node) => node switch
    {
        null => null,
        DomainWorkspaces.PaneLeaf leaf => new PaneLeafRecord(leaf.Id, leaf.ModuleIds, leaf.ActiveModuleId),
        DomainWorkspaces.PaneSplit split => new PaneSplitRecord(split.Id, split.Orientation, split.Ratio, ToRecord(split.First)!, ToRecord(split.Second)!),
        _ => throw new InvalidOperationException($"Unknown pane node type '{node.GetType()}'."),
    };

    private static string DefaultWorkspacesFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "workspaces", "workspaces.json");

    private static string DefaultStateFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "workspaces", "state.json");
}
