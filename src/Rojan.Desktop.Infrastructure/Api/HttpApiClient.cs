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
/// from the <c>ROJAN_API_BASE_URL</c> environment variable rather than a
/// hardcoded value (Phase 25's "no hardcoded values") - unset today,
/// since no backend exists yet, which is why every real call currently
/// fails with a clear <see cref="ApiConnectivityException"/> instead of
/// silently succeeding against nothing.
///
/// Sprint 7 Commit 1: <see cref="PutAsync{TRequest, TResponse}"/>/
/// <see cref="DeleteAsync{TResponse}"/> round out the CRUD surface (see
/// <see cref="IApiClient"/>'s own doc comment), sharing this class's
/// existing <see cref="SendAsync{TResponse}"/> pipeline unchanged - every
/// pipeline concern listed above still applies to them exactly as it
/// already does to <see cref="GetAsync{TResponse}"/>/<see cref="PostAsync{TRequest, TResponse}"/>.
/// <see cref="EnsureNotAuthenticationFailure"/> was pulled out of
/// <see cref="SendOnceAsync{TResponse}"/> as its own named guard (same
/// shape as <see cref="EnsureConnectivity"/>/<see cref="EnsureBaseAddressConfigured"/>)
/// purely so a future commit that adds a refresh-and-retry-once flow on
/// 401 has one obvious, already-isolated method to extend - no behavior
/// changed here, refresh-on-401 is explicitly out of this commit's scope.
/// </summary>
public sealed class HttpApiClient : IApiClient, IDisposable
{
    private const string BaseAddressEnvironmentVariable = "ROJAN_API_BASE_URL";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan TimeoutSeconds = TimeSpan.FromSeconds(30);

    private readonly IConnectivityService _connectivityService;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ISessionService _sessionService;
    private readonly HttpClient _httpClient;

    public HttpApiClient(IConnectivityService connectivityService, IRetryPolicy retryPolicy, ISessionService sessionService)
        : this(connectivityService, retryPolicy, sessionService, new HttpClientHandler(), GetConfiguredBaseAddress())
    {
    }

    /// <summary>
    /// Test-only seam (see <c>Rojan.Desktop.Infrastructure.csproj</c>'s
    /// <c>InternalsVisibleTo</c>) - lets
    /// <c>Infrastructure.Tests.Api.HttpApiClientTests</c> substitute a
    /// fake <see cref="HttpMessageHandler"/> and an explicit
    /// <paramref name="baseAddress"/>, so tests never depend on the
    /// process-wide <c>ROJAN_API_BASE_URL</c> environment variable (fragile
    /// to set/unset per test, especially under parallel test execution).
    /// The public constructor above always goes through this one too - its
    /// own behavior is completely unchanged, still reading the environment
    /// variable and using a real <see cref="HttpClientHandler"/>.
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

    public void Dispose() => _httpClient.Dispose();

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        EnsureConnectivity();
        EnsureBaseAddressConfigured();

        try
        {
            return await _retryPolicy
                .ExecuteAsync(ct => SendOnceAsync<TResponse>(requestFactory, ct), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not ApiException)
        {
            throw MapException(exception);
        }
    }

    private async Task<ApiResponse<TResponse>> SendOnceAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
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
            EnsureNotAuthenticationFailure(response, request.RequestUri);

            var body = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResponseFactory.Failure<TResponse>((int)response.StatusCode, body);
            }

            var data = string.IsNullOrWhiteSpace(body)
                ? default
                : JsonSerializer.Deserialize<TResponse>(body, SerializerOptions);

            return ApiResponseFactory.Success(data!, (int)response.StatusCode);
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
            throw new ApiConnectivityException($"No API base address is configured - set the '{BaseAddressEnvironmentVariable}' environment variable.");
        }
    }

    /// <summary>
    /// Sprint 7 Commit 1: extracted from <see cref="SendOnceAsync{TResponse}"/>
    /// as its own named guard, same shape as <see cref="EnsureConnectivity"/>/
    /// <see cref="EnsureBaseAddressConfigured"/> - a pure extraction, no
    /// behavior change (same exception type, same message). Exists as its
    /// own method purely so a future commit adding a refresh-and-retry-once
    /// flow on 401 has one obvious, already-isolated place to change -
    /// that flow itself is explicitly out of this commit's scope.
    /// </summary>
    private static void EnsureNotAuthenticationFailure(HttpResponseMessage response, Uri? requestUri)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            throw new ApiAuthenticationException($"Request to '{requestUri}' was rejected with status {(int)response.StatusCode}.");
        }
    }

    private static Uri? GetConfiguredBaseAddress()
    {
        var baseAddress = Environment.GetEnvironmentVariable(BaseAddressEnvironmentVariable);
        return string.IsNullOrWhiteSpace(baseAddress) ? null : new Uri(baseAddress, UriKind.Absolute);
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
