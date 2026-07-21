namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>
/// Pure tree-manipulation logic for a workspace's secondary-pane tree - a
/// deliberate deviation from this codebase's usual "Domain is just data +
/// repository contract" minimalism, same reasoning as <c>Bookings.BookingRules</c>:
/// split/close/resize correctness (never producing an empty split, always
/// clamping a ratio) has to live somewhere Application/Presentation can
/// both trust without re-deriving it, and Domain is where every other
/// vertical slice in this app puts that kind of rule. No I/O, no WPF, no
/// randomness beyond the caller-supplied id generator - fully unit-testable.
/// </summary>
public static class WorkspaceRules
{
    /// <summary>A split can never be squeezed past this fraction for either side - a pane that thin is unusable.</summary>
    public const double MinRatio = 0.15;

    public const double MaxRatio = 0.85;

    public const double DefaultRatio = 0.5;

    public const double MinDockSize = 200;

    public const double MaxDockSize = 560;

    public const double DefaultDockSize = 280;

    public static double ClampRatio(double ratio) => Math.Clamp(ratio, MinRatio, MaxRatio);

    public static double ClampDockSize(double size) => Math.Clamp(size, MinDockSize, MaxDockSize);

    /// <summary>A trimmed, never-blank workspace name - falls back to <paramref name="fallback"/> if <paramref name="name"/> is null/blank after trimming.</summary>
    public static string NormalizeName(string? name, string fallback)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) ? fallback : trimmed;
    }

    /// <summary>Builds a brand-new workspace in its default state: the primary pane shows <paramref name="primaryModuleId"/>, no secondary panes, no docked panels, no floating windows.</summary>
    public static WorkspaceLayout CreateDefault(string id, string name, string primaryModuleId, DateTimeOffset now, bool isDefault = false) =>
        new(id, name, primaryModuleId, null, [], [], now, now, isDefault);

    /// <summary>
    /// Splits <paramref name="targetLeafId"/> (or, if not found/not supplied, the first leaf in the tree) into a new
    /// <see cref="PaneSplit"/> whose second child is a fresh leaf holding <paramref name="newModuleId"/>. If
    /// <paramref name="root"/> is <see langword="null"/> (the workspace has no secondary panes yet), the split's first
    /// child captures <paramref name="primaryModuleId"/> - splitting for the first time takes what the primary pane was
    /// already showing rather than discarding it.
    /// </summary>
    public static PaneNode Split(PaneNode? root, string? targetLeafId, string primaryModuleId, string newModuleId, PaneOrientation orientation, Func<string> newId)
    {
        var newLeaf = new PaneLeaf(newId(), [newModuleId], newModuleId);

        if (root is null)
        {
            var firstLeaf = new PaneLeaf(newId(), [primaryModuleId], primaryModuleId);
            return new PaneSplit(newId(), orientation, DefaultRatio, firstLeaf, newLeaf);
        }

        var effectiveTargetId = targetLeafId is not null && FindLeaf(root, targetLeafId) is not null
            ? targetLeafId
            : AllLeaves(root).First().Id;

        return Transform(root, effectiveTargetId, leaf => new PaneSplit(newId(), orientation, DefaultRatio, leaf, newLeaf)) ?? root;
    }

    /// <summary>Adds <paramref name="moduleId"/> as a new tab in the leaf identified by <paramref name="leafId"/> (or activates it, if already open there).</summary>
    public static PaneNode OpenTab(PaneNode root, string leafId, string moduleId) =>
        Transform(root, leafId, leaf => leaf.ModuleIds.Contains(moduleId)
            ? leaf with { ActiveModuleId = moduleId }
            : leaf with { ModuleIds = [.. leaf.ModuleIds, moduleId], ActiveModuleId = moduleId })
        ?? root;

    /// <summary>Activates <paramref name="moduleId"/> within the leaf identified by <paramref name="leafId"/>, if it's open there.</summary>
    public static PaneNode SetActiveTab(PaneNode root, string leafId, string moduleId) =>
        Transform(root, leafId, leaf => leaf.ModuleIds.Contains(moduleId) ? leaf with { ActiveModuleId = moduleId } : leaf)
        ?? root;

    /// <summary>
    /// Closes the <paramref name="moduleId"/> tab in the leaf identified by <paramref name="leafId"/>. If that leaf
    /// becomes empty, the leaf itself is removed and its parent <see cref="PaneSplit"/> collapses into whichever
    /// sibling remains - a split can never end up with a missing child. Returns <see langword="null"/> if the whole
    /// tree becomes empty (the last tab in the last leaf was closed).
    /// </summary>
    public static PaneNode? CloseTab(PaneNode root, string leafId, string moduleId) =>
        Transform(root, leafId, leaf =>
        {
            var remaining = leaf.ModuleIds.Where(id => id != moduleId).ToList();
            if (remaining.Count == 0)
            {
                return null;
            }

            var activeModuleId = leaf.ActiveModuleId == moduleId ? remaining[0] : leaf.ActiveModuleId;
            return leaf with { ModuleIds = remaining, ActiveModuleId = activeModuleId };
        });

    /// <summary>Removes every open tab for <paramref name="moduleId"/> across the whole tree - used when a module is floated out, so it isn't left open in both places at once.</summary>
    public static PaneNode? CloseModuleEverywhere(PaneNode root, string moduleId)
    {
        PaneNode? current = root;
        foreach (var leaf in AllLeaves(root).ToList())
        {
            if (current is null)
            {
                break;
            }

            if (leaf.ModuleIds.Contains(moduleId) && FindLeaf(current, leaf.Id) is not null)
            {
                current = CloseTab(current, leaf.Id, moduleId);
            }
        }

        return current;
    }

    /// <summary>Applies a new split ratio to the <see cref="PaneSplit"/> identified by <paramref name="splitId"/>, clamped to <see cref="MinRatio"/>/<see cref="MaxRatio"/>.</summary>
    public static PaneNode Resize(PaneNode node, string splitId, double ratio) => node switch
    {
        PaneSplit split when split.Id == splitId => split with { Ratio = ClampRatio(ratio) },
        PaneSplit split => split with { First = Resize(split.First, splitId, ratio), Second = Resize(split.Second, splitId, ratio) },
        _ => node,
    };

    /// <summary>Finds the leaf with the given id, or <see langword="null"/> if the tree doesn't contain one (including when <paramref name="node"/> itself is <see langword="null"/>).</summary>
    public static PaneLeaf? FindLeaf(PaneNode? node, string leafId) => node switch
    {
        null => null,
        PaneLeaf leaf => leaf.Id == leafId ? leaf : null,
        PaneSplit split => FindLeaf(split.First, leafId) ?? FindLeaf(split.Second, leafId),
        _ => null,
    };

    /// <summary>Every leaf in the tree, in a stable left-to-right/top-to-bottom order - used for tab-cycling and for enumerating what's open when restoring or resolving a workspace.</summary>
    public static IEnumerable<PaneLeaf> AllLeaves(PaneNode? node)
    {
        switch (node)
        {
            case null:
                yield break;
            case PaneLeaf leaf:
                yield return leaf;
                yield break;
            case PaneSplit split:
                foreach (var leaf in AllLeaves(split.First))
                {
                    yield return leaf;
                }

                foreach (var leaf in AllLeaves(split.Second))
                {
                    yield return leaf;
                }

                yield break;
        }
    }

    /// <summary>
    /// Walks the tree looking for the leaf whose <see cref="PaneNode.Id"/> matches <paramref name="targetLeafId"/>,
    /// applying <paramref name="transform"/> to it in place. Returning <see langword="null"/> from
    /// <paramref name="transform"/> removes that leaf - its parent <see cref="PaneSplit"/> then collapses into
    /// whichever sibling subtree remains (itself possibly rebuilt by an outer recursive call), and if both children of
    /// the root disappear, the whole tree collapses to <see langword="null"/>.
    /// </summary>
    private static PaneNode? Transform(PaneNode node, string targetLeafId, Func<PaneLeaf, PaneNode?> transform)
    {
        switch (node)
        {
            case PaneLeaf leaf when leaf.Id == targetLeafId:
                return transform(leaf);
            case PaneLeaf leaf:
                return leaf;
            case PaneSplit split:
                var first = Transform(split.First, targetLeafId, transform);
                var second = Transform(split.Second, targetLeafId, transform);
                if (first is null)
                {
                    return second;
                }

                if (second is null)
                {
                    return first;
                }

                return ReferenceEquals(first, split.First) && ReferenceEquals(second, split.Second)
                    ? split
                    : split with { First = first, Second = second };
            default:
                throw new InvalidOperationException($"Unknown {nameof(PaneNode)} type '{node.GetType()}'.");
        }
    }
}
