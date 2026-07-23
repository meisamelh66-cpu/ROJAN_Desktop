namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>
/// Phase C-2 (Staff Status Panel): a plain presentation-only model - no
/// repository/DTO of its own, same reasoning <see cref="QuickActionItem"/>
/// already documents, since there is no real staff-workload data source
/// anywhere in this app yet. Ring color is derived from
/// <see cref="ProgressPercentage"/> by a converter (StaffProgressColorConverter),
/// not stored here, so this stays pure display data rather than a UI-typed
/// (Brush) field.
/// </summary>
public sealed class StaffStatusItem
{
    public required string Name { get; init; }

    public required string ServiceLabel { get; init; }

    public required bool IsOnline { get; init; }

    public required int ProgressPercentage { get; init; }

    public string? RemainingTimeText { get; init; }
}
