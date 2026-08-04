namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/mine</c> returns a list of - matches ROJAN_Backend's <c>SalonResponse</c> field-for-field (see <c>api/salon/SalonDtos.kt</c>). Only <see cref="Id"/> is actually consumed today (by <see cref="Salons.ISalonContextService"/>); the rest is kept for completeness/future use, matching this namespace's existing "mirror the backend shape, not just what's used yet" convention.</summary>
public sealed record SalonResponse(
    string Id,
    string OwnerId,
    string Name,
    string? Description,
    string Phone,
    string? Email,
    string Address,
    bool Active);
