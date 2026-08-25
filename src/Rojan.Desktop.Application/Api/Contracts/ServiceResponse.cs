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

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/categories</c> returns a list of - matches ROJAN_Backend's <c>ServiceCategoryResponse</c> field-for-field (see <c>api/salon/ServiceCategoryDtos.kt</c>). Service Catalog Authoring: now also consumed for <c>Id</c>+<c>Name</c> together, via <see cref="Domain.Services.IServiceRepository.GetCategoriesAsync"/> - see <see cref="ServiceResponse"/>'s own doc comment for why this has to be enumerated at all.</summary>
public sealed record ServiceCategoryResponse(string Id, string SalonId, string Name, string? Description, bool Active);

/// <summary>Service Catalog Authoring: the request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/categories/{categoryId}/services</c> expects - matches ROJAN_Backend's <c>CreateServiceRequest</c> field-for-field (see <c>api/salon/ServiceDtos.kt</c>). No category field - the target category is the URL's own <c>{categoryId}</c> segment, never part of the body.</summary>
public sealed record CreateServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);

/// <summary>Service Catalog Authoring: the request body <c>PUT {ApiVersion.BasePath()}/salons/{salonId}/categories/{categoryId}/services/{serviceId}</c> expects - matches ROJAN_Backend's <c>UpdateServiceRequest</c> field-for-field. No status field (deactivation is a separate <c>DELETE</c>) and no category field (ROJAN_Backend has no way to change a service's category at all - see <c>Domain.Services.IServiceRepository</c>'s own doc comment).</summary>
public sealed record UpdateServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);
