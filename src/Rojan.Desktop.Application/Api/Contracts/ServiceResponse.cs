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

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/categories</c> returns a list of - matches ROJAN_Backend's <c>ServiceCategoryResponse</c> field-for-field (see <c>api/salon/ServiceCategoryDtos.kt</c>). Service Catalog Management: both <see cref="Id"/> and <see cref="Name"/> are consumed now, to populate a Create-Service category picker - see <see cref="ServiceResponse"/>'s own doc comment for why this has to be enumerated at all.</summary>
public sealed record ServiceCategoryResponse(string Id, string SalonId, string Name, string? Description, bool Active);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/categories/{categoryId}/services</c> accepts - matches ROJAN_Backend's <c>CreateServiceRequest</c> field-for-field (see <c>api/salon/ServiceDtos.kt</c>).</summary>
public sealed record CreateServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);

/// <summary>The request body <c>PUT {ApiVersion.BasePath()}/salons/{salonId}/categories/{categoryId}/services/{serviceId}</c> accepts - matches ROJAN_Backend's <c>UpdateServiceRequest</c> field-for-field. Deliberately has no status/category field at all - see <c>BackendServiceRepository.DeactivateServiceAsync</c>'s own doc comment for why status changes go through the dedicated deactivate endpoint instead, and why there is no way to move a service between categories or to reactivate one through any endpoint.</summary>
public sealed record UpdateServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);
