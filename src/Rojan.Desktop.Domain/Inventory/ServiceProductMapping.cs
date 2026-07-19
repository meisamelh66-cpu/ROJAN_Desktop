namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// A single service-to-product mapping - which product (and how much of
/// it) a given service consumes. <see cref="ServiceId"/>/<see cref="ServiceName"/>
/// are free-form, unvalidated references, same reasoning as
/// <c>Services.SpecialistService.SpecialistId</c>/<c>SpecialistName</c>:
/// this vertical slice deliberately does not depend on <c>Domain.Services</c>
/// (per the Independence goal in docs/architecture/00-overview.md §2) -
/// linking to a real Service record is a future integration point, not
/// built here. <see cref="ProductId"/>/<see cref="ProductName"/> are real
/// references within this same slice.
/// </summary>
public sealed record ServiceProductMapping(string Id, string ServiceId, string ServiceName, string ProductId, string ProductName, int QuantityPerService);
