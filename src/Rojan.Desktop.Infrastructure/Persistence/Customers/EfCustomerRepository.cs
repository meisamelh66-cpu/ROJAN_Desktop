using Microsoft.EntityFrameworkCore;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>
/// Sprint 6 Commit 2: real EF Core-backed <see cref="DomainCustomers.ICustomerRepository"/> -
/// the first Domain module to move off its <c>Fake*Repository</c> onto
/// <see cref="RojanDbContext"/> (see that class's own doc comment for the
/// one-module-at-a-time plan Commit 1 established). Every method opens its
/// own short-lived <see cref="RojanDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/> rather than holding one for
/// this repository's lifetime - this class is registered as a DI
/// singleton (matching every other repository in this app), and EF Core's
/// own <see cref="DbContext"/> is neither long-lived-safe nor thread-safe,
/// so a fresh context per call is the correct shape here, not an
/// oversight.
///
/// Behavior is a deliberate, field-for-field mirror of
/// <c>FakeCustomerRepository</c> - same read ordering
/// (<see cref="GetNotesAsync"/> newest-first, <see cref="GetTagsAsync"/>
/// oldest-first, <see cref="GetActivityAsync"/> newest-first), same "full
/// replace, no field preserved" <see cref="UpdateCustomerAsync"/>
/// semantics, same "throw if not found" on an update to a missing id. No
/// Organization/Branch filtering happens here - see
/// <c>Application.Customers.CustomerQueryService.ScopeToCurrentSession</c>'s
/// own doc comment for why that stays an Application concern, never a
/// repository one.
/// </summary>
public sealed class EfCustomerRepository : DomainCustomers.ICustomerRepository
{
    private readonly IDbContextFactory<RojanDbContext> _contextFactory;

    public EfCustomerRepository(IDbContextFactory<RojanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DomainCustomers.Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.Customers.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(CustomerEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainCustomers.Customer?> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : CustomerEntityMapper.MapToDomain(entity);
    }

    public async Task<IReadOnlyList<DomainCustomers.CustomerNote>> GetNotesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Ordered client-side (LINQ to Objects, after ToListAsync) rather
        // than via a translated ORDER BY - the Sqlite EF Core provider
        // cannot translate ordering over DateTimeOffset columns
        // ("SQLite does not support expressions of type 'DateTimeOffset'
        // in ORDER BY clauses"). Fine at this scale: FakeCustomerRepository
        // already orders every one of these lists in plain LINQ to Objects
        // too, never pushed to a real backend.
        var entities = await context.CustomerNotes
            .AsNoTracking()
            .Where(note => note.CustomerId == customerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .OrderByDescending(note => note.CreatedAt)
            .Select(CustomerEntityMapper.MapToDomain)
            .ToList();
    }

    public async Task<IReadOnlyList<DomainCustomers.CustomerTag>> GetTagsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // See GetNotesAsync's own comment - same Sqlite DateTimeOffset
        // ORDER BY limitation, same client-side-ordering fix.
        var entities = await context.CustomerTags
            .AsNoTracking()
            .Where(tag => tag.CustomerId == customerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .OrderBy(tag => tag.CreatedAt)
            .Select(CustomerEntityMapper.MapToDomain)
            .ToList();
    }

    public async Task<IReadOnlyList<DomainCustomers.CustomerActivity>> GetActivityAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // See GetNotesAsync's own comment - same Sqlite DateTimeOffset
        // ORDER BY limitation, same client-side-ordering fix.
        var entities = await context.CustomerActivities
            .AsNoTracking()
            .Where(activity => activity.CustomerId == customerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .OrderByDescending(activity => activity.OccurredAt)
            .Select(CustomerEntityMapper.MapToDomain)
            .ToList();
    }

    public async Task<DomainCustomers.Customer> CreateCustomerAsync(DomainCustomers.Customer customer, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.Customers.Add(CustomerEntityMapper.MapToEntity(customer));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return customer;
    }

    public async Task<DomainCustomers.Customer> UpdateCustomerAsync(DomainCustomers.Customer customer, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Customers
            .FirstOrDefaultAsync(existing => existing.Id == customer.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer '{customer.Id}' was not found.");

        CustomerEntityMapper.ApplyTo(entity, customer);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return customer;
    }

    public async Task<DomainCustomers.CustomerNote> AddNoteAsync(DomainCustomers.CustomerNote note, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.CustomerNotes.Add(CustomerEntityMapper.MapToEntity(note));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return note;
    }

    public async Task<DomainCustomers.CustomerTag> AddTagAsync(DomainCustomers.CustomerTag tag, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.CustomerTags.Add(CustomerEntityMapper.MapToEntity(tag));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tag;
    }

    public async Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.CustomerTags
            .FirstOrDefaultAsync(tag => tag.CustomerId == customerId && tag.Id == tagId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        context.CustomerTags.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DomainCustomers.CustomerActivity> AddActivityAsync(DomainCustomers.CustomerActivity activity, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.CustomerActivities.Add(CustomerEntityMapper.MapToEntity(activity));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return activity;
    }
}
