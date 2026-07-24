namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Sprint 7 Commit 5: the wire shape a future backend's error response
/// body is expected to take for a non-2xx response. Not parsed anywhere
/// yet - <c>Infrastructure.Api.HttpApiClient</c> still treats a non-2xx
/// body as an opaque string for <see cref="ApiResponse{T}.ErrorMessage"/>,
/// which stays true today (no runtime behavior changes in this commit).
/// This is the target shape a future commit can deserialize into once a
/// real backend exists and its actual error format is known - defined now
/// so that work starts from an agreed contract. <see cref="Details"/> is
/// optional (defaults to <see langword="null"/>) since not every error a
/// backend returns will have anything beyond a code/message to add.
/// </summary>
public sealed record ApiErrorResponse(string Code, string Message, string? Details = null);
