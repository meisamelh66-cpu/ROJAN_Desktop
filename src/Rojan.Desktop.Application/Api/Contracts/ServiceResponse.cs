namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/categories/{categoryId}/services/{serviceId}</c> (and its list form) returns - matches ROJAN_Backend's <c>ServiceResponse</c> field-for-field (see <c>api/salon/ServiceDtos.kt</c>), used by <c>BackendBookingRepository</c> to resolve a booking's service display name and price. There is no flat "get service by id" endpoint on the backend - a service only resolves through its owning category, which is why <c>BackendBookingRepository</c> fetches every category then every category's services rather than a single lookup.</summary>
public sealed record ServiceResponse(
    string Id,
    string SalonId,
    string CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool Active);

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/categories</c> returns a list of - matches ROJAN_Backend's <c>ServiceCategoryResponse</c> field-for-field (see <c>api/salon/ServiceCategoryDtos.kt</c>). Only <see cref="Id"/> is consumed today - see <see cref="ServiceResponse"/>'s own doc comment for why this has to be enumerated at all.</summary>
public sealed record ServiceCategoryResponse(string Id, string SalonId, string Name, string? Description, bool Active);
