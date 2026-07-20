namespace Rojan.Desktop.Domain.Organizations;

/// <summary>A branch's booking-policy constraints - display/administered data only; this phase does not wire these into Booking's own validation (see the phase doc's Migration Notes).</summary>
public sealed record AppointmentRules(int MinNoticeHours, int MaxAdvanceBookingDays, bool AllowSameDayBooking);
