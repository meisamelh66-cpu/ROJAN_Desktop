using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Persistence.Bookings;

/// <summary>Domain&lt;-&gt;persistence-entity mapping for the Bookings vertical slice - internal, only <see cref="EfBookingRepository"/> calls it, same convention as every other Domain&lt;-&gt;entity mapper in this codebase (<see cref="Customers.CustomerEntityMapper"/>, <see cref="Specialists.SpecialistEntityMapper"/>, <see cref="Services.ServiceEntityMapper"/>).</summary>
internal static class BookingEntityMapper
{
    public static DomainBookings.Booking MapToDomain(BookingEntity entity) => new(
        entity.Id,
        entity.CustomerId,
        entity.CustomerName,
        entity.ServiceId,
        entity.ServiceName,
        entity.SpecialistId,
        entity.SpecialistName,
        entity.ScheduledAt,
        entity.DurationMinutes,
        entity.Price,
        entity.Status,
        entity.Notes,
        entity.OrganizationId,
        entity.BranchId);

    public static BookingEntity MapToEntity(DomainBookings.Booking booking) => new()
    {
        Id = booking.Id,
        CustomerId = booking.CustomerId,
        CustomerName = booking.CustomerName,
        ServiceId = booking.ServiceId,
        ServiceName = booking.ServiceName,
        SpecialistId = booking.SpecialistId,
        SpecialistName = booking.SpecialistName,
        ScheduledAt = booking.ScheduledAt,
        DurationMinutes = booking.DurationMinutes,
        Price = booking.Price,
        Status = booking.Status,
        Notes = booking.Notes,
        OrganizationId = booking.OrganizationId,
        BranchId = booking.BranchId,
    };
}
