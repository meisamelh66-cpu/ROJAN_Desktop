namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Sprint 7 Commit 5: API Contracts Preparation. This namespace holds the
/// strongly-typed shapes a future backend's HTTP surface will speak -
/// prepared now, ahead of any actual backend/controller/routing (there is
/// still none - see <see cref="IApiClient"/>'s own doc comment), so that
/// work starts from an agreed contract instead of inventing request/
/// response shapes ad hoc once a server exists.
///
/// <see cref="ApiVersion"/> itself is metadata only - a version token
/// future request paths would be composed with (e.g.
/// <c>$"{ApiVersion.BasePath()}/customers"</c>), not a router or a change
/// to any path <see cref="IApiClient"/> callers pass today (still plain,
/// caller-supplied literal strings - see e.g.
/// <c>Infrastructure.Sync.SyncQueueService</c>'s <c>"sync/operations"</c>).
///
/// Per this commit's own scope ("reuse existing models, avoid duplicate
/// representations"), most modules do not get a new contract type here at
/// all - their existing Application-layer DTOs already are the wire
/// shape a future backend integration would reuse as-is, being plain
/// records of primitives/enums with no Domain/Infrastructure dependency:
/// <see cref="Customers.CustomerDto"/>/<see cref="Customers.CreateCustomerRequest"/>/
/// <see cref="Customers.UpdateCustomerRequest"/>,
/// <see cref="Specialists.SpecialistDto"/>/<see cref="Specialists.CreateSpecialistRequest"/>/
/// <see cref="Specialists.UpdateSpecialistRequest"/>, and
/// <see cref="Services.ServiceDto"/> for their respective modules, and
/// <see cref="Domain.Security.PendingSyncOperation"/> for the sync queue's
/// own operation envelope (see
/// <c>Infrastructure.Sync.SyncQueueService</c> - it is already posted
/// directly as the request body). Only what genuinely does not exist yet
/// gets a new type in this namespace: <see cref="ApiErrorResponse"/> (no
/// structured error shape exists today - see its own doc comment) and
/// <see cref="AuthRefreshRequest"/>/<see cref="AuthRefreshResponse"/> (no
/// wire shape exists for refresh at all today, since
/// <see cref="Security.ISessionService.RefreshAsync"/> rotates tokens
/// entirely locally - see its own doc comment).
/// </summary>
public static class ApiVersion
{
    public const string V1 = "v1";

    public static string BasePath(string version = V1) => $"/api/{version}";
}
