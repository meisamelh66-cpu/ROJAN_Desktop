namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Sprint 7 Commit 5: the request body a future
/// <c>POST {ApiVersion.BasePath()}/auth/refresh</c> endpoint would accept.
/// <see cref="Security.ISessionService.RefreshAsync"/> rotates tokens
/// entirely locally today (see <c>Infrastructure.Security.LocalSessionService</c>'s
/// own doc comment) - no backend call is made, so nothing constructs this
/// yet. Defined now so a future backend integration has an agreed-upon
/// shape rather than inventing one ad hoc; carries only the current
/// <see cref="Domain.Security.RefreshToken.Value"/>, matching the minimal
/// input <see cref="Security.ISessionService.RefreshAsync"/> itself
/// already needs (the current session's refresh token) to do the same
/// rotation on a real server.
/// </summary>
public sealed record AuthRefreshRequest(string RefreshToken);
