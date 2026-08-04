namespace Rojan.Desktop.Application.Api;

/// <summary>
/// Phase 25: Secure API Foundation. The one abstraction every future
/// backend call goes through - no module gets its own
/// <c>HttpClient</c>/networking logic (Phase 25.6's "no duplicated
/// networking logic"). The concrete implementation
/// (<c>Infrastructure.Api.HttpApiClient</c>) composes connectivity
/// checking, retry, authentication-header attachment, timeout, and
/// exception mapping around a single <c>HttpClient</c> - callers only
/// ever see this interface and <see cref="ApiResponse{T}"/>/
/// <see cref="ApiException"/>.
///
/// Sprint 7 Commit 1: <see cref="PutAsync{TRequest, TResponse}"/>/
/// <see cref="DeleteAsync{TResponse}"/> round out the CRUD surface
/// alongside the original <see cref="GetAsync{TResponse}"/>/
/// <see cref="PostAsync{TRequest, TResponse}"/> pair - same request/
/// response shape, no new abstraction, since every future backend
/// resource will need update/remove operations, not just read/create.
///
/// Sprint 7 Commit 5: <c>path</c> on every method here stays a plain,
/// caller-supplied literal string - no behavior change in this commit.
/// <see cref="Contracts.ApiVersion"/> is the version token a future
/// caller would compose that path with once real endpoints exist (e.g.
/// <c>$"{ApiVersion.BasePath()}/customers"</c>); see its own doc comment
/// for the full set of request/response contracts this commit prepares
/// (<c>Contracts.ApiErrorResponse</c>, <c>Contracts.AuthRefreshRequest</c>/
/// <c>Contracts.AuthRefreshResponse</c>) and for why most modules reuse
/// their existing Application-layer DTOs as the request/response type
/// arguments here rather than getting new duplicate contract types.
/// </summary>
public interface IApiClient
{
    public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Owner App Booking Integration: rounds out the CRUD surface with the
    /// one verb ROJAN_Backend's booking status-transition endpoints use
    /// (<c>PATCH .../confirm</c>, <c>.../cancel</c>, <c>.../complete</c>) -
    /// same reasoning as Sprint 7 Commit 1 adding <see cref="PutAsync{TRequest, TResponse}"/>/
    /// <see cref="DeleteAsync{TResponse}"/>. No request body parameter,
    /// since every backend endpoint that needs this verb today sends none.
    /// </summary>
    public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Owner App Customer CRM Integration: a second <c>PatchAsync</c>
    /// overload for the one case the body-less one above doesn't cover -
    /// ROJAN_Backend's <c>PATCH /customers/{id}</c> is a genuine partial
    /// update that takes a JSON body, unlike the parameterless booking
    /// status-transition endpoints. Same "add the one verb shape a new
    /// integration needs" reasoning as every earlier <see cref="IApiClient"/>
    /// addition.
    /// </summary>
    public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);
}
