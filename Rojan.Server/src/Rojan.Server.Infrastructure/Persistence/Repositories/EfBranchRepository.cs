using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>Default <see cref="IBranchRepository"/> - same "inject the scoped DbContext directly" reasoning as <see cref="EfOrganizationRepository"/>'s own doc comment.</summary>
public sealed class EfBranchRepository : IBranchRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfBranchRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Branch?> GetByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
        _dbContext.Branches.FirstOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        await _dbContext.Branches
            .Where(branch => branch.OrganizationId == organizationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
