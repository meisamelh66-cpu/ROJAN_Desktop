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
    {
        _connectivityService = connectivityService;
        _retryPolicy = retryPolicy;
        _sessionService = sessionService;

        _httpClient = new HttpClient();
        var baseAddress = Environment.GetEnvironmentVariable(BaseAddressEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(baseAddress))
        {
            _httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
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
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                throw new ApiAuthenticationException($"Request to '{request.RequestUri}' was rejected with status {(int)response.StatusCode}.");
            }

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
