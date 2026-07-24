using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>
/// Default <see cref="IOrganizationRepository"/>. Injects
/// <see cref="RojanServerDbContext"/> directly (scoped per HTTP request by
/// ASP.NET Core's own DI container - <c>AddDbContext</c>, not
/// <c>AddDbContextFactory</c>) rather than the desktop solution's own
/// <c>IDbContextFactory</c>-per-call pattern; that pattern exists
/// specifically because the desktop app's container is all-singleton with
/// no per-operation scope of its own. A web API already gets that scope
/// for free per request, so the idiomatic ASP.NET Core approach applies
/// here instead.
/// </summary>
public sealed class EfOrganizationRepository : IOrganizationRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfOrganizationRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Organization> CreateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _dbContext.Organizations.Add(organization);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return organization;
    }

    public Task<Organization?> GetByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        _dbContext.Organizations.FirstOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken);
}
