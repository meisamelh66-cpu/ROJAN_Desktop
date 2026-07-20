namespace Rojan.Desktop.Application.Organizations;

public sealed record AppointmentRulesDto(int MinNoticeHours, int MaxAdvanceBookingDays, bool AllowSameDayBooking);
