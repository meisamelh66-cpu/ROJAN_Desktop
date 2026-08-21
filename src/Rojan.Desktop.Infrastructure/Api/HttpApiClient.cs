using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Api;

/// <summary>
/// Default <see cref="IApiClient"/>. Owns a single internal
/// <see cref="HttpClient"/> (disposed with this instance - no
/// <c>IHttpClientFactory</c> needed for a desktop app with exactly one
/// backend to call) and composes every pipeline concern Phase 25.6 names
/// around it: <see cref="EnsureConnectivity"/> (Connectivity
/// Handler - short-circuits before attempting a request that is known to
/// fail), <see cref="AttachAuthenticationHeader"/> (Authentication
/// Handler - a Bearer token from <see cref="ISessionService"/> when one
/// exists), <see cref="IRetryPolicy"/> (Retry Handler), a per-call
/// <see cref="TimeoutSeconds"/> bound via a linked
/// <see cref="CancellationTokenSource"/> (Timeout support, layered on top
/// of - not replacing - caller-supplied cancellation), and
/// <see cref="MapException"/> (Exception Mapping). The base address comes
/// from <see cref="IApiEnvironmentService"/> rather than a hardcoded value
/// (Phase 25's "no hardcoded values") - see that service's own doc comment
/// for the Development/Production/env-var resolution order.
///
/// Sprint 7 Commit 1: <see cref="PutAsync{TRequest, TResponse}"/>/
/// <see cref="DeleteAsync{TResponse}"/> round out the CRUD surface (see
/// <see cref="IApiClient"/>'s own doc comment), sharing this class's
/// existing <see cref="SendAsync{TResponse}"/> pipeline unchanged - every
/// pipeline concern listed above still applies to them exactly as it
/// already does to <see cref="GetAsync{TResponse}"/>/<see cref="PostAsync{TRequest, TResponse}"/>.
///
/// Sprint 7 Commit 2: a 401 no longer throws immediately from inside
/// <see cref="SendOnceAsync{TResponse}"/> (superseding Commit 1's
/// <c>EnsureNotAuthenticationFailure</c> guard, which existed only to mark
/// this exact seam) - it now flows through as an ordinary
/// <see cref="ApiResponse{T}"/> failure so <see cref="EnsureAuthenticatedAsync{TResponse}"/>
/// can react to it *outside* <see cref="IRetryPolicy"/>'s wrapping. That
/// separation matters: <see cref="RetryPolicy"/> retries on any exception,
/// so if a 401 still threw from inside the retried delegate, the generic
/// policy would burn up to 5 backoff-delayed attempts on a failure a token
/// refresh could have fixed on the first try, before a refresh was even
/// attempted. A 403 is never retried at all (a fresh access token cannot
/// fix an authorization failure, only an authentication one) and still
/// throws immediately, same as before. See
/// <see cref="EnsureAuthenticatedAsync{TResponse}"/>'s own doc comment for
/// the full refresh-and-retry-once flow.
/// </summary>
public sealed class HttpApiClient : IApiClient, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan TimeoutSeconds = TimeSpan.FromSeconds(30);

    private readonly IConnectivityService _connectivityService;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ISessionService _sessionService;
    private readonly HttpClient _httpClient;

    /// <summary>Owner App Login Experience: base address now comes from <see cref="IApiEnvironmentService"/> (Development/Production, persisted, env-var-overridable) instead of reading <c>ROJAN_API_BASE_URL</c> directly - see that service's own doc comment for the full resolution order.</summary>
    public HttpApiClient(IConnectivityService connectivityService, IRetryPolicy retryPolicy, ISessionService sessionService, IApiEnvironmentService apiEnvironmentService)
        : this(connectivityService, retryPolicy, sessionService, new HttpClientHandler(), apiEnvironmentService.ResolvedBaseAddress)
    {
    }

    /// <summary>
    /// Test-only seam (see <c>Rojan.Desktop.Infrastructure.csproj</c>'s
    /// <c>InternalsVisibleTo</c>) - lets
    /// <c>Infrastructure.Tests.Api.HttpApiClientTests</c> substitute a
    /// fake <see cref="HttpMessageHandler"/> and an explicit
    /// <paramref name="baseAddress"/>, so tests never depend on
    /// <see cref="IApiEnvironmentService"/>'s persisted settings file or the
    /// process-wide <c>ROJAN_API_BASE_URL</c> environment variable (fragile
    /// to set/unset per test, especially under parallel test execution).
    /// The public constructor above always goes through this one too - its
    /// own behavior is completely unchanged, still resolving via
    /// <see cref="IApiEnvironmentService"/> and using a real
    /// <see cref="HttpClientHandler"/>.
    /// </summary>
    internal HttpApiClient(
        IConnectivityService connectivityService,
        IRetryPolicy retryPolicy,
        ISessionService sessionService,
        HttpMessageHandler handler,
        Uri? baseAddress)
    {
        _connectivityService = connectivityService;
        _retryPolicy = retryPolicy;
        _sessionService = sessionService;

        _httpClient = new HttpClient(handler);
        if (baseAddress is not null)
        {
            _httpClient.BaseAddress = baseAddress;
        }
    }

    public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json"),
            },
            cancellationToken);

    public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json"),
            },
            cancellationToken);

    public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Delete, path), cancellationToken);

    public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Patch, path), cancellationToken);

    public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Patch, path)
            {
                Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json"),
            },
            cancellationToken);

    /// <summary>Desktop Productionization Sprint 1: see <see cref="IApiClient.GetBytesAsync"/> - same pipeline as every JSON method above, routed through <see cref="SendBytesAsync"/> instead of the generic <see cref="SendAsync{TResponse}"/> since a PNG body must never go through <see cref="JsonSerializer"/>.</summary>
    public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
        SendBytesAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public void Dispose() => _httpClient.Dispose();

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        EnsureConnectivity();
        EnsureBaseAddressConfigured();

        try
        {
            var response = await _retryPolicy
                .ExecuteAsync(ct => SendOnceAsync<TResponse>(requestFactory, ct), cancellationToken)
                .ConfigureAwait(false);

            return await EnsureAuthenticatedAsync(response, requestFactory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not ApiException)
        {
            throw MapException(exception);
        }
    }

    /// <summary>Desktop Productionization Sprint 1: the <c>byte[]</c> counterpart to <see cref="SendAsync{TResponse}"/> - identical connectivity/retry/401-refresh/exception-mapping shell, routed through <see cref="SendOnceBytesAsync"/> instead of the JSON-decoding <see cref="SendOnceAsync{TResponse}"/>.</summary>
    private async Task<ApiResponse<byte[]>> SendBytesAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        EnsureConnectivity();
        EnsureBaseAddressConfigured();

        try
        {
            var response = await _retryPolicy
                .ExecuteAsync(ct => SendOnceBytesAsync(requestFactory, ct), cancellationToken)
                .ConfigureAwait(false);

            return await EnsureAuthenticatedBytesAsync(response, requestFactory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not ApiException)
        {
            throw MapException(exception);
        }
    }

    /// <summary>
    /// Sprint 7 Commit 2: reacts to a 401/403 that has already flowed
    /// through <see cref="_retryPolicy"/> as an ordinary
    /// <see cref="ApiResponse{T}"/> failure (see this class's own doc
    /// comment for why it must be data, not an exception, at this point).
    /// A 403 can never be fixed by refreshing the access token - only a
    /// fresh sign-in can - so it throws immediately without attempting a
    /// refresh. A 401 gets exactly one refresh-and-retry: <see cref="RefreshSessionOrThrowAsync"/>
    /// rotates the session's tokens via <see cref="ISessionService.RefreshAsync"/>
    /// (or throws <see cref="ApiAuthenticationException"/> if that fails),
    /// then the request is sent exactly one more time through the same
    /// <see cref="_retryPolicy"/>-wrapped path - <see cref="AttachAuthenticationHeader"/>
    /// picks up the newly-refreshed token automatically since it reads
    /// <see cref="ISessionService.CurrentAccessToken"/> fresh on every call,
    /// no extra plumbing needed. If that single retry is still 401/403,
    /// this method throws rather than recursing, which is what guarantees
    /// "retry maximum once" and rules out an infinite refresh loop.
    /// </summary>
    private async Task<ApiResponse<TResponse>> EnsureAuthenticatedAsync<TResponse>(
        ApiResponse<TResponse> response,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == (int)HttpStatusCode.Forbidden)
        {
            throw new ApiAuthenticationException($"Request was rejected with status {response.StatusCode}.");
        }

        if (response.StatusCode != (int)HttpStatusCode.Unauthorized)
        {
            return response;
        }

        await RefreshSessionOrThrowAsync(cancellationToken).ConfigureAwait(false);

        var retried = await _retryPolicy
            .ExecuteAsync(ct => SendOnceAsync<TResponse>(requestFactory, ct), cancellationToken)
            .ConfigureAwait(false);

        if (retried.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden)
        {
            throw new ApiAuthenticationException($"Request was still rejected with status {retried.StatusCode} after refreshing the session.");
        }

        return retried;
    }

    /// <summary>Desktop Productionization Sprint 1: the <c>byte[]</c> counterpart to <see cref="EnsureAuthenticatedAsync{TResponse}"/> - identical 403-throws-immediately/401-refresh-and-retry-once semantics, see that method's own doc comment for the full reasoning.</summary>
    private async Task<ApiResponse<byte[]>> EnsureAuthenticatedBytesAsync(
        ApiResponse<byte[]> response,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == (int)HttpStatusCode.Forbidden)
        {
            throw new ApiAuthenticationException($"Request was rejected with status {response.StatusCode}.");
        }

        if (response.StatusCode != (int)HttpStatusCode.Unauthorized)
        {
            return response;
        }

        await RefreshSessionOrThrowAsync(cancellationToken).ConfigureAwait(false);

        var retried = await _retryPolicy
            .ExecuteAsync(ct => SendOnceBytesAsync(requestFactory, ct), cancellationToken)
            .ConfigureAwait(false);

        if (retried.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden)
        {
            throw new ApiAuthenticationException($"Request was still rejected with status {retried.StatusCode} after refreshing the session.");
        }

        return retried;
    }

    private async Task RefreshSessionOrThrowAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sessionService.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ApiAuthenticationException("Session refresh failed after an authentication failure - sign in again.", exception);
        }
    }

    private async Task<ApiResponse<TResponse>> SendOnceAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        var (isSuccess, statusCode, bytes) = await SendOnceRawAsync(requestFactory, cancellationToken).ConfigureAwait(false);
        var body = bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);

        if (!isSuccess)
        {
            return ApiResponseFactory.Failure<TResponse>(statusCode, body);
        }

        var data = string.IsNullOrWhiteSpace(body)
            ? default
            : JsonSerializer.Deserialize<TResponse>(body, SerializerOptions);

        return ApiResponseFactory.Success(data!, statusCode);
    }

    /// <summary>Desktop Productionization Sprint 1: the <c>byte[]</c> counterpart to <see cref="SendOnceAsync{TResponse}"/> - shares <see cref="SendOnceRawAsync"/>'s connectivity/timeout/auth-header/send core, skips the JSON decode step entirely (a PNG body is not UTF-8 text).</summary>
    private async Task<ApiResponse<byte[]>> SendOnceBytesAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        var (isSuccess, statusCode, bytes) = await SendOnceRawAsync(requestFactory, cancellationToken).ConfigureAwait(false);

        return isSuccess
            ? ApiResponseFactory.Success(bytes, statusCode)
            : ApiResponseFactory.Failure<byte[]>(statusCode, bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes));
    }

    /// <summary>
    /// Desktop Productionization Sprint 1: the one HTTP-send core every
    /// <c>SendOnce*</c> variant shares - timeout-wrapped cancellation,
    /// auth header attachment, the actual <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/>
    /// call, and reading the raw response bytes. Extracted from what used
    /// to be <see cref="SendOnceAsync{TResponse}"/>'s own body so the QR
    /// Ecosystem feature's raw-<c>byte[]</c> path
    /// (<see cref="SendOnceBytesAsync"/>) doesn't duplicate it - only the
    /// final "decode as JSON vs. return raw" step differs between callers.
    /// </summary>
    private async Task<(bool IsSuccess, int StatusCode, byte[] Bytes)> SendOnceRawAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeoutSeconds);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        using var request = requestFactory();
        AttachAuthenticationHeader(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ApiTimeoutException($"Request to '{request.RequestUri}' timed out after {TimeoutSeconds.TotalSeconds:N0}s.");
        }

        using (response)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(linkedCts.Token).ConfigureAwait(false);
            return (response.IsSuccessStatusCode, (int)response.StatusCode, bytes);
        }
    }

    private void EnsureConnectivity()
    {
        if (_connectivityService.CurrentState == ConnectionState.Offline)
        {
            throw new ApiConnectivityException("No network connection is available.");
        }
    }

    private void EnsureBaseAddressConfigured()
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new ApiConnectivityException("No API base address is configured - set an API environment in Settings, or the 'ROJAN_API_BASE_URL' environment variable.");
        }
    }

    private void AttachAuthenticationHeader(HttpRequestMessage request)
    {
        var accessToken = _sessionService.CurrentAccessToken;
        if (accessToken is not null && !accessToken.IsExpired(DateTimeOffset.UtcNow))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
        }
    }

    private static ApiException MapException(Exception exception) => exception switch
    {
        HttpRequestException httpException => new ApiConnectivityException($"Request failed: {httpException.Message}", httpException),
        _ => new ApiException($"Request failed: {exception.Message}", exception),
    };
}
