using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Creates a
/// brand-new <c>Domain.Authentication.Organization</c> and its first
/// <c>Domain.Authentication.User</c> (stamped
/// <see cref="Domain.Authentication.UserRoles.Owner"/>) in one call -
/// there is no separate "create organization" then "add user" pair of
/// operations in this commit (see <c>IAuthenticationService</c>'s own doc
/// comment). Bound directly from the request body by
/// <c>Api.Controllers.AuthController</c> - the same "reuse the
/// Application-layer DTO as the wire contract" choice the desktop
/// solution's own <c>Application.Api.Contracts</c> namespace already
/// established, rather than a parallel API-layer request type.
/// </summary>
public sealed record RegisterOrganizationOwnerRequest(
    [Required, MinLength(1)] string OrganizationName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MinLength(1)] string FullName);
