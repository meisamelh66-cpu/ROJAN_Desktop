namespace Rojan.Server.Domain.Authentication;

/// <summary>Persistence contract for <see cref="Branch"/>. No create/update operations yet - this commit's registration flow never assigns a branch (see <see cref="User.BranchId"/>'s own doc comment), so only the read path used by <see cref="UserRules.IsValidBranchAssignment"/> exists so far.</summary>
public interface IBranchRepository
{
    public Task<Branch?> GetByIdAsync(string branchId, CancellationToken cancellationToken = default);
}
