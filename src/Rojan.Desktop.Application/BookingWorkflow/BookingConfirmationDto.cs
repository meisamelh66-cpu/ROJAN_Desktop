namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>Result of a successful <see cref="IBookingWorkflowService.CreateBookingAsync"/> call - what the wizard's success-confirmation step displays.</summary>
public sealed record BookingConfirmationDto(
    string BookingId,
    string CustomerName,
    string ServiceName,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Price);
