namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The tenant
/// boundary - every <see cref="Branch"/> and <see cref="User"/> belongs to
/// exactly one <see cref="Organization"/>, and nothing in this schema ever
/// lets a query cross that boundary implicitly (see
/// <see cref="UserRules.IsValidBranchAssignment"/>). Created as part of
/// <c>Application.Authentication.IAuthenticationService</c>'s "register
/// organization owner" flow - there is no separate "create organization"
/// operation in this commit, since standalone tenant management is out of
/// scope (see the solution's own README).
/// </summary>
public sealed record Organization(string Id, string Name, DateTimeOffset CreatedAt);
