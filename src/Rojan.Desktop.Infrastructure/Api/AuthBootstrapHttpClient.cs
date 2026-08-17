using System.Net;
using System.Text;
using System.Text.Json;
using Rojan.Desktop.Application.Api;

namespace Rojan.Desktop.Infrastructure.Api;

/// <summary>
/// A minimal, standalone HTTP client used ONLY for the two auth-bootstrap
/// calls (login, refresh) that must never go through <see cref="HttpApiClient"/>'s
/// generic pipeline. That pipeline's 401-response handling calls
/// <c>ISessionService.RefreshAsync</c> - if refresh is itself what's
/// failing (an invalid/expired refresh token, or a fresh login attempt
/// with the wrong password also returning 401), routing either call back
/// through <see cref="HttpApiClient"/> would either re-enter
/// <c>RefreshAsync</c> from inside itself (unbounded recursion) or surface
/// a misleading "session refresh failed" message for what is actually a
/// plain login failure. This client deliberately has none of
/// <see cref="HttpApiClient"/>'s other pipeline concerns (connectivity
/// short-circuit, retry policy, auth-header attachment - there is no
/// session token yet for either of these two calls anyway) - just a direct
/// POST and JSON [de]serialization against the same
/// <see cref="IApiEnvironmentService"/>-resolved base address
/// <see cref="HttpApiClient"/> uses.
/// </summary>
public sealed class AuthBootstrapHttpClient : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan TimeoutSeconds = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;

    public AuthBootstrapHttpClient(IApiEnvironmentService apiEnvironmentService)
        : this(new HttpClientHandler(), apiEnvironmentService.ResolvedBaseAddress)
    {
    }

    /// <summary>Test-only seam, mirroring <see cref="HttpApiClient"/>'s own internal constructor (see <c>Rojan.Desktop.Infrastructure.csproj</c>'s <c>InternalsVisibleTo</c>).</summary>
    internal AuthBootstrapHttpClient(HttpMessageHandler handler, Uri? baseAddress)
    {
        _httpClient = new HttpClient(handler) { Timeout = TimeoutSeconds };
        if (baseAddress is not null)
        {
            _httpClient.BaseAddress = baseAddress;
        }
    }

    public void Dispose() => _httpClient.Dispose();

    /// <summary>
    /// Throws <see cref="ApiAuthenticationException"/> for a 401/403 (the
    /// caller's credentials/refresh token were rejected - never retried,
    /// since there is nothing to refresh here), <see cref="ApiRateLimitException"/>
    /// for a 429 (Desktop OTP Authentication Migration - request/resend/verify
    /// rate limiting), <see cref="ApiConnectivityException"/> if no base
    /// address is configured or the transport fails, and <see cref="ApiException"/>
    /// for any other non-2xx response.
    /// </summary>
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new ApiConnectivityException("No API base address is configured - set an API environment in Settings, or the 'ROJAN_API_BASE_URL' environment variable.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json"),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiTimeoutException($"Request to '{path}' timed out after {TimeoutSeconds.TotalSeconds:N0}s.");
        }
        catch (HttpRequestException httpException)
        {
            throw new ApiConnectivityException($"Request failed: {httpException.Message}", httpException);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new ApiAuthenticationException($"Request was rejected with status {(int)response.StatusCode}: {responseBody}", (int)response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ApiRateLimitException($"Request was rate-limited: {responseBody}");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException($"Request failed with status {(int)response.StatusCode}: {responseBody}");
            }

            return JsonSerializer.Deserialize<TResponse>(responseBody, SerializerOptions)
                ?? throw new ApiException("Response body was empty or 'null'.");
        }
    }
}
