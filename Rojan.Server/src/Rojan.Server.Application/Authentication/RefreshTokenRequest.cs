using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Authentication;

/// <summary>Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Carries the raw refresh token value the client currently holds - never persisted or looked up raw, only ever hashed first (see <c>Domain.Authentication.RefreshToken.TokenHash</c>'s own doc comment).</summary>
public sealed record RefreshTokenRequest([Required] string RefreshToken);
