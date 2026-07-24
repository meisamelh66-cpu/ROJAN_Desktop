using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Persistence.Bookings;

/// <summary>
/// EF Core persistence model for a booking - field-for-field mirror of
/// <see cref="DomainBookings.Booking"/>, kept as its own mutable class
/// rather than mapping the Domain record directly - same "Infrastructure
/// owns the translation, Domain stays persistence-ignorant" reasoning
/// <see cref="Customers.CustomerEntity"/>/<see cref="Specialists.SpecialistEntity"/>/
/// <see cref="Services.ServiceEntity"/> already establish.
///
/// <see cref="CustomerId"/>/<see cref="ServiceId"/>/<see cref="SpecialistId"/>
/// are plain, unconstrained text columns - no foreign keys to the
/// Customers/Specialists/Services tables. This mirrors
/// <see cref="DomainBookings.Booking"/>'s own doc comment exactly
/// ("free-form, unvalidated references... this vertical slice
/// deliberately does not depend on Domain.Customers, Domain.Services, or
/// Domain.Specialists") - a real foreign key here would bake a hard
/// database-level coupling between vertical slices that the Domain model
/// explicitly avoids at the code level, the one relationship this
/// commit's own Phase 3 instructions call out as one to *not* invent.
/// </summary>
public sealed class BookingEntity
{
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string SpecialistId { get; set; } = string.Empty;

    public string SpecialistName { get; set; } = string.Empty;

    public DateTimeOffset ScheduledAt { get; set; }

    public int DurationMinutes { get; set; }

    public string Price { get; set; } = string.Empty;

    public DomainBookings.BookingStatus Status { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string OrganizationId { get; set; } = string.Empty;

    public string BranchId { get; set; } = string.Empty;
}
