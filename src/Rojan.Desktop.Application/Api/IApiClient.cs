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
/// </summary>
public interface IApiClient
{
    public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);

    public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default);
}
