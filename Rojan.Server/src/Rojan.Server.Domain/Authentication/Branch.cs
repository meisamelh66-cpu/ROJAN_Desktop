namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. A location/
/// division within an <see cref="Organization"/> - <see cref="User.BranchId"/>
/// is optional (a user may belong to the organization only, with no
/// specific branch), but when set it must reference a branch within the
/// same <see cref="OrganizationId"/> as the user (enforced by
/// <see cref="UserRules.IsValidBranchAssignment"/>, not by this record
/// itself - a record cannot see its siblings). No branch-management
/// operations exist yet in this commit (create/rename/delete branches) -
/// out of scope, same as standalone organization management.
/// </summary>
public sealed record Branch(string Id, string OrganizationId, string Name, DateTimeOffset CreatedAt);
