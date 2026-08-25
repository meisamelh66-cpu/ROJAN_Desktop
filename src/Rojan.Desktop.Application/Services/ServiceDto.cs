namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Application-layer shape of a catalog service, mapped from
/// <see cref="Rojan.Desktop.Domain.Services.Service"/> by <see cref="ServiceMapper"/>.
/// <see cref="CategoryName"/> mirrors <see cref="Rojan.Desktop.Domain.Services.Service.CategoryName"/>'s
/// own doc comment - null for local/EF-backed data, the backend's real
/// category text otherwise. Service Catalog Authoring: <see cref="CategoryId"/>
/// mirrors <see cref="Rojan.Desktop.Domain.Services.Service.CategoryId"/> -
/// the real, backend-owned category id, read-only from this layer's own
/// point of view (see that field's own doc comment for why).
/// <see cref="PriceValue"/> is <see cref="Price"/> pre-parsed to a raw
/// <see cref="decimal"/> via <see cref="ServicePriceParser"/> at mapping
/// time - Presentation cannot call that parser itself (internal to this
/// assembly), and needs a raw decimal to populate an editable price field
/// and to build <see cref="CreateServiceRequest"/>/<see cref="UpdateServiceRequest"/>
/// without ever formatting/parsing a display string on a write path.
/// </summary>
public sealed record ServiceDto(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description,
    string? CategoryName = null,
    string CategoryId = "",
    decimal PriceValue = 0m);
