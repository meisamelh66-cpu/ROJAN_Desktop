using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Authentication;

/// <summary>Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. No organization/tenant identifier - <see cref="Email"/> alone must resolve to exactly one user (see <c>Domain.Authentication.IUserRepository.GetByEmailAsync</c>'s own doc comment: email is globally unique across tenants).</summary>
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
