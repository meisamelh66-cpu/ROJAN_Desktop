namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>
/// One node of a workspace's secondary-pane tree - either a <see cref="PaneLeaf"/>
/// (a tab strip of open modules) or a <see cref="PaneSplit"/> (two child nodes
/// divided by a resizable gutter). A binary tree, not a flat list, so an
/// arbitrary number of nested splits (split, then split one side again) is
/// representable without a separate "layout grid" concept. The primary
/// (sidebar-driven) content region is deliberately not part of this tree -
/// see <see cref="WorkspaceLayout.PrimaryModuleId"/>.
/// </summary>
public abstract record PaneNode(string Id);

/// <summary>A single pane holding one or more open module tabs, at most one of them active at a time.</summary>
public sealed record PaneLeaf(string Id, IReadOnlyList<string> ModuleIds, string? ActiveModuleId) : PaneNode(Id);

/// <summary>Two child panes divided by a resizable gutter. <see cref="Ratio"/> is the fraction of space <see cref="First"/> occupies, always clamped to <see cref="WorkspaceRules.MinRatio"/>..<see cref="WorkspaceRules.MaxRatio"/> by <see cref="WorkspaceRules"/>.</summary>
public sealed record PaneSplit(string Id, PaneOrientation Orientation, double Ratio, PaneNode First, PaneNode Second) : PaneNode(Id);
