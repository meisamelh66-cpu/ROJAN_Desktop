namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// A single specialist (staff member) record, as returned by
/// <see cref="ISpecialistRepository"/>. Not linked to
/// <c>Domain.Bookings.Booking.SpecialistName</c> by anything beyond
/// matching seed-data names - this vertical slice deliberately does not
/// depend on <c>Domain.Bookings</c> (per the Independence goal in
/// docs/architecture/00-overview.md §2); a real cross-slice link is a
/// future integration point, not built here.
/// </summary>
public sealed record Specialist(
    string Id,
    string FullName,
    string Title,
    string Email,
    string Phone,
    SpecialistStatus Status,
    string Bio);
