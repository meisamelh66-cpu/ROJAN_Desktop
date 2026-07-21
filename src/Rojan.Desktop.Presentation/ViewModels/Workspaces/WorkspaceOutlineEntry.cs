namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>One row in the Workspace Outline panel - either an open tab (<paramref name="LeafId"/> set, <paramref name="IsFloating"/> false) or a floating window (<paramref name="LeafId"/> null, <paramref name="IsFloating"/> true).</summary>
public sealed record WorkspaceOutlineEntry(string? LeafId, string ModuleId, string Title, bool IsFloating);
