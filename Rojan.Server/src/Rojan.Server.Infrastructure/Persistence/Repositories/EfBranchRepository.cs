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
}
