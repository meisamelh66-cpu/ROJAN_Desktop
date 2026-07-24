namespace Rojan.Desktop.Presentation.ViewModels.Calendar;

/// <summary>
/// Which layout <c>CalendarPage</c> renders - Sprint 2 Commit 4 foundation.
/// <see cref="Week"/> reuses the existing per-slot generation/conflict/break
/// logic (via <c>ICalendarQueryService.GetWeeklyAvailabilityAsync</c>, Sprint
/// 2 Commit 3) but is deliberately a simple per-day summary grid, not yet
/// the redesigned appointment-card layout - that is a later commit's scope.
/// </summary>
public enum CalendarViewMode
{
    Day,
    Week,
}
