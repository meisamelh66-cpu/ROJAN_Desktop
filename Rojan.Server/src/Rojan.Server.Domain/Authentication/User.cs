namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Belongs to
/// exactly one <see cref="Organization"/> (<see cref="OrganizationId"/>,
/// required) and optionally one <see cref="Branch"/> within it
/// (<see cref="BranchId"/> - see <see cref="UserRules.IsValidBranchAssignment"/>
/// for the consistency rule between the two). <see cref="PasswordHash"/>
/// is never the plain password - see
/// <c>Infrastructure.Security.Pbkdf2PasswordHasher</c>'s own doc comment
/// for the hashing scheme. <see cref="Role"/> is a single string, not a
/// collection or an enum - full multi-role RBAC/a permissions matrix is
/// explicitly out of this commit's scope (see <see cref="UserRoles"/>'s
/// own doc comment); <c>Application.Authentication.AuthenticationResult.Roles</c>
/// wraps this one value in a list so the wire shape does not need to
/// change when real multi-role support eventually arrives.
/// </summary>
public sealed record User(
    string Id,
    string OrganizationId,
    string? BranchId,
    string Email,
    string PasswordHash,
    string FullName,
    string Role,
    DateTimeOffset CreatedAt);
