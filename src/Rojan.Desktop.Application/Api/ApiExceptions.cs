namespace Rojan.Desktop.Application.Api;

/// <summary>Phase 25: Secure API Foundation. Base type every <see cref="IApiClient"/> failure that is not an ordinary non-2xx response (see <see cref="ApiResponse{T}"/>) is mapped to - a caller can catch this one type and still branch on the concrete subtype when it matters.</summary>
public class ApiException : Exception
{
    public ApiException(string message)
        : base(message)
    {
    }

    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Thrown instead of attempting a request when <c>IConnectivityService</c> already reports <see cref="Rojan.Desktop.Domain.Security.ConnectionState.Offline"/>, and when the underlying transport fails with a connection-level error.</summary>
public sealed class ApiConnectivityException : ApiException
{
    public ApiConnectivityException(string message)
        : base(message)
    {
    }

    public ApiConnectivityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Thrown when a request exceeds its configured timeout - distinct from a caller-initiated cancellation, which surfaces as <see cref="OperationCanceledException"/> instead.</summary>
public sealed class ApiTimeoutException : ApiException
{
    public ApiTimeoutException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Desktop OTP Authentication Migration: thrown when the backend responds
/// 429 - distinct from a generic non-2xx response because a rate-limit
/// rejection is a case the OTP request/resend/verify flow needs to show a
/// specific "too many attempts, wait and retry" message for, not the same
/// catch-all as an unrecognized failure. Which specific backend rate limit
/// this was (<c>OTP_REQUEST_RATE_LIMITED</c> vs <c>OTP_VERIFY_RATE_LIMITED</c>)
/// is not carried on the exception itself - the calling ViewModel method
/// (request/resend vs verify) already identifies which one applies, since
/// each hits a different endpoint.
/// </summary>
public sealed class ApiRateLimitException : ApiException
{
    public ApiRateLimitException(string message)
        : base(message)
    {
    }
}

/// <summary>Thrown when the backend responds 401/403 - distinct from other non-2xx responses (which surface as a failed <see cref="ApiResponse{T}"/>) because an auth failure is something the authentication handler should react to (e.g. by expiring the session), not just report.</summary>
public sealed class ApiAuthenticationException : ApiException
{
    public ApiAuthenticationException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>Sprint 7 Commit 2: used when a 401's refresh-and-retry attempt fails because <c>ISessionService.RefreshAsync</c> itself threw (e.g. an expired refresh token) - the original exception is preserved as <see cref="Exception.InnerException"/> rather than discarded.</summary>
    public ApiAuthenticationException(string message, Exception innerException, int? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Authentication Error Handling Alignment (Phase 1): the raw HTTP status
    /// (401 or 403) that caused this exception, when the throw site knows it -
    /// <see langword="null"/> for call sites that don't have one (e.g. a
    /// synthetic "session refresh itself failed" exception with no single
    /// status code of its own). Lets a caller (e.g.
    /// <c>MobileOtpLoginViewModel.VerifyCodeAsync</c>) distinguish "invalid/
    /// expired code" (401) from "not authorized to sign in this way" (403)
    /// without parsing the response body or assuming a backend-specific
    /// error-code contract - deliberately the smallest possible addition,
    /// not a general error-code/body-parsing overhaul (out of scope for this
    /// phase).
    /// </summary>
    public int? StatusCode { get; }
}
