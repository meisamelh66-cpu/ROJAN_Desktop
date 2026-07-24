namespace Rojan.Desktop.Application.Api;

/// <summary>
/// Phase 25: Secure API Foundation. Uniform result shape for every
/// <see cref="IApiClient"/> call - callers branch on <see cref="IsSuccess"/>
/// rather than catching exceptions for expected failure modes (a non-2xx
/// response), reserving thrown <see cref="ApiException"/>s for
/// connectivity/timeout/cancellation.
///
/// Sprint 7 Commit 5: <see cref="ErrorMessage"/> stays a raw opaque string
/// today - no runtime behavior change in this commit.
/// <see cref="Contracts.ApiErrorResponse"/> is the structured shape a
/// future backend's error body is expected to take, and the target a
/// future commit can deserialize <see cref="ErrorMessage"/>'s source body
/// into once a real backend exists.
/// </summary>
public sealed record ApiResponse<T>(bool IsSuccess, T? Data, int? StatusCode, string? ErrorMessage);

/// <summary>Factory helpers for <see cref="ApiResponse{T}"/> - a separate non-generic type rather than static members on the generic record itself (CA1000: generic types should not expose static factory members, since <c>ApiResponse&lt;T&gt;.Success(...)</c> forces callers to state <c>T</c> explicitly at every call site anyway).</summary>
public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, int statusCode) => new(true, data, statusCode, null);

    public static ApiResponse<T> Failure<T>(int? statusCode, string errorMessage) => new(false, default, statusCode, errorMessage);
}
