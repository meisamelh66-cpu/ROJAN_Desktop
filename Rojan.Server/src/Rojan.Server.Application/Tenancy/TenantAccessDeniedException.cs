namespace Rojan.Server.Application.Tenancy;

/// <summary>
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Thrown by
/// <see cref="ITenantService"/> whenever the current request's tenant
/// context (see <see cref="ITenantContext"/>) does not resolve to a
/// usable tenant - the organization no longer exists or is suspended, or
/// the branch does not belong to that organization or is inactive. This
/// is distinct from <c>Application.Authentication.AuthenticationException</c>:
/// the caller presented a perfectly valid, correctly-signed token
/// (authentication succeeded) but what it claims about tenancy no longer
/// holds (authorization/tenancy failure) - <c>Api.Controllers.TenantController</c>
/// maps this to 401 anyway today since there is no more specific status
/// this foundation commit needs yet, but the two failure modes are kept
/// as separate exception types so a future commit can map them
/// differently without redesigning authentication.
/// </summary>
public sealed class TenantAccessDeniedException(string message) : Exception(message);
