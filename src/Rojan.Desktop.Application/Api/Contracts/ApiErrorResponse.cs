namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The error response body ROJAN_Backend returns for every non-2xx
/// response except a bare 401 (see remarks) - matches that project's
/// <c>ApiError</c> field-for-field (see <c>api/common/GlobalExceptionHandler.kt</c>).
/// Not parsed anywhere yet - <c>Infrastructure.Api.HttpApiClient</c> still
/// treats a non-2xx body as an opaque string for
/// <see cref="ApiResponse{T}.ErrorMessage"/> (a future commit can
/// deserialize into this shape once a caller actually needs
/// <see cref="ErrorCode"/> to branch on, rather than just display the raw
/// message). A bare 401 (missing/invalid/expired bearer token) is the one
/// exception: the backend's security layer returns a fixed, smaller body -
/// <c>{"errorCode":"AUTH_UNAUTHORIZED","message":"Authentication required"}</c> -
/// which this same shape still deserializes correctly since
/// <see cref="Status"/>/<see cref="Error"/>/<see cref="Path"/>/<see cref="TraceId"/>
/// are all optional.
/// </summary>
public sealed record ApiErrorResponse(
    string ErrorCode,
    string Message,
    int? Status = null,
    string? Error = null,
    string? Path = null,
    string? TraceId = null);
