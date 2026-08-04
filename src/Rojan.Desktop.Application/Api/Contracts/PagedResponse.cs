namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The pagination envelope every paged ROJAN_Backend list endpoint returns
/// (e.g. <c>GET /api/v1/salons/{salonId}/bookings</c>) - matches that
/// project's <c>PagedResponse&lt;T&gt;</c> field-for-field (see
/// <c>api/common/PagedResponse.kt</c>).
/// </summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Content, int Page, int Size, long TotalElements, int TotalPages);
