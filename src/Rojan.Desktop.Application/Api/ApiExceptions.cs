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

/// <summary>Thrown when the backend responds 401/403 - distinct from other non-2xx responses (which surface as a failed <see cref="ApiResponse{T}"/>) because an auth failure is something the authentication handler should react to (e.g. by expiring the session), not just report.</summary>
public sealed class ApiAuthenticationException : ApiException
{
    public ApiAuthenticationException(string message)
        : base(message)
    {
    }
}
