namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Sprint 7 Commit 5: the response body the same future
/// <c>POST {ApiVersion.BasePath()}/auth/refresh</c> endpoint (see
/// <see cref="AuthRefreshRequest"/>) would return - a fresh access/refresh
/// token pair. Field shape mirrors <see cref="Domain.Security.AuthToken"/>/
/// <see cref="Domain.Security.RefreshToken"/> exactly (<c>Value</c>/
/// <c>IssuedAt</c>/<c>ExpiresAt</c> each) so a future backend integration
/// can map this response straight onto those Domain types with no
/// translation gap.
/// </summary>
public sealed record AuthRefreshResponse(
    string AccessToken,
    DateTimeOffset AccessTokenIssuedAt,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenIssuedAt,
    DateTimeOffset RefreshTokenExpiresAt);
