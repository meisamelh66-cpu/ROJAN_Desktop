using Microsoft.EntityFrameworkCore;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Persistence.Bookings;

/// <summary>
/// Sprint 6 Commit 5: real EF Core-backed <see cref="DomainBookings.IBookingRepository"/> -
/// the fourth Domain module moved off its <c>Fake*Repository</c> onto
/// <see cref="RojanDbContext"/>, same shape <see cref="Customers.EfCustomerRepository"/>/
/// <see cref="Specialists.EfSpecialistRepository"/>/<see cref="Services.EfServiceRepository"/>
/// already establish (short-lived <see cref="RojanDbContext"/> per call via
/// <see cref="IDbContextFactory{TContext}"/>, registered as a DI
/// singleton).
///
/// Unlike Customers/Specialists, there is no general
/// <c>UpdateBookingAsync</c> - <see cref="DomainBookings.IBookingRepository"/>
/// only has two narrow updates
/// (<see cref="UpdateBookingStatusAsync"/>/<see cref="RescheduleBookingAsync"/>),
/// each touching exactly one field, matching
/// <c>FakeBookingRepository</c>'s own <c>with { Status = status }</c>/
/// <c>with { ScheduledAt = newScheduledAt }</c> semantics precisely -
/// nothing else on the tracked row changes. Status transition validation
/// (<c>Domain.Bookings.BookingRules.IsValidTransition</c>) and
/// double-booking conflict checks stay entirely in
/// <c>Application.Bookings.BookingCommandService</c> - this repository
/// never re-validates either, same "dumb data access, Application owns
/// the rules" contract every Ef*Repository in this app follows. No
/// Organization/Branch filtering happens here either - see
/// <c>Application.Bookings.BookingQueryService.ScopeToCurrentSession</c>'s
/// own doc comment for why that stays an Application concern.
/// </summary>
public sealed class EfBookingRepository : DomainBookings.IBookingRepository
{
    private readonly IDbContextFactory<RojanDbContext> _contextFactory;

    public EfBookingRepository(IDbContextFactory<RojanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DomainBookings.Booking>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.Bookings.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(BookingEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainBookings.Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : BookingEntityMapper.MapToDomain(entity);
    }

    public async Task<DomainBookings.Booking> CreateBookingAsync(DomainBookings.Booking booking, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.Bookings.Add(BookingEntityMapper.MapToEntity(booking));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return booking;
    }

    public async Task<DomainBookings.Booking> UpdateBookingStatusAsync(string bookingId, DomainBookings.BookingStatus status, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Bookings
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        entity.Status = status;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return BookingEntityMapper.MapToDomain(entity);
    }

    public async Task<DomainBookings.Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Bookings
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        entity.ScheduledAt = newScheduledAt;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return BookingEntityMapper.MapToDomain(entity);
    }
}
