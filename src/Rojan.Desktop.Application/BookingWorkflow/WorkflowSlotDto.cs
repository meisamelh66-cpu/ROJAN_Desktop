namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>One pickable time slot for the wizard's time-slot-selection step - the "available slot query" this phase requires. Already filtered to Available-only (unlike the Calendar module's own grid, which shows Booked slots too); the wizard is meant to be a simpler, guided pick-a-time flow.</summary>
public sealed record WorkflowSlotDto(DateTimeOffset Start, DateTimeOffset End);
