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

/// <summary>Phase 1.2 Owner App Create Salon Flow: the request body <c>POST {ApiVersion.BasePath()}/salons</c> accepts - matches ROJAN_Backend's <c>CreateSalonRequest</c> field-for-field (see <c>api/salon/SalonDtos.kt</c>). <see cref="Name"/>/<see cref="Phone"/>/<see cref="Address"/> are required (backend-validated <c>@NotBlank</c>); <see cref="Description"/>/<see cref="Email"/> are optional.</summary>
public sealed record CreateSalonRequest(string Name, string? Description, string Phone, string? Email, string Address);
