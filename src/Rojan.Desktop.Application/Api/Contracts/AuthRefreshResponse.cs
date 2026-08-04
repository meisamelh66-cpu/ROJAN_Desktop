namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Superseded by <see cref="AuthResponse"/>: now that a real backend exists,
/// <c>POST {ApiVersion.BasePath()}/auth/refresh</c> is confirmed to return
/// the exact same shape as <c>/auth/login</c> (including the nested
/// <c>user</c> object, and no separate <c>IssuedAt</c> fields) - the
/// Infrastructure layer's real session service uses <see cref="AuthResponse"/>
/// for both calls. Left in place, unreferenced, rather than deleted,
/// matching this codebase's convention for a superseded-but-still-
/// informative type (see <c>Infrastructure.Dashboard.FakeDashboardRepository</c>'s
/// own DI comment).
/// </summary>
public sealed record AuthRefreshResponse(
    string AccessToken,
    DateTimeOffset AccessTokenIssuedAt,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenIssuedAt,
    DateTimeOffset RefreshTokenExpiresAt);
