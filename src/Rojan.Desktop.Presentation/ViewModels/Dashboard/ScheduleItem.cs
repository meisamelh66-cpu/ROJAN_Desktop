namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>Phase C-2 (Daily Schedule panel): a plain presentation-only model, same reasoning as <see cref="StaffStatusItem"/>.</summary>
public sealed class ScheduleItem
{
    public required string Time { get; init; }

    public required string SpecialistName { get; init; }

    public required string ServiceLabel { get; init; }

    public required bool IsCurrent { get; init; }
}
