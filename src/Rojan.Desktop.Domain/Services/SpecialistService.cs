namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// A single specialist-to-service assignment - which specialist is
/// qualified/assigned to perform a given service. <see cref="SpecialistId"/>/
/// <see cref="SpecialistName"/> are free-form, unvalidated references, same
/// reasoning as <c>Bookings.Booking.SpecialistName</c>: this vertical slice
/// deliberately does not depend on <c>Domain.Specialists</c> (per the
/// Independence goal in docs/architecture/00-overview.md §2) - linking to
/// a real Specialist record is a future integration point, not built here.
/// </summary>
public sealed record SpecialistService(string Id, string ServiceId, string SpecialistId, string SpecialistName);
