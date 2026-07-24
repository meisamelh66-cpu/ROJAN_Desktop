namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Combinable filter criteria for <see cref="IServiceQueryService.SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/> -
/// every field is optional and ANDed together when present, same shape as
/// <c>Bookings.BookingSearchFilter</c>/<c>Customers.CustomerSearchFilter</c>.
/// A filter with every field left at its default is equivalent to no
/// filtering at all: <see cref="ServiceQueryService.SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/>
/// returns the exact same result set, in the same order, as
/// <see cref="IServiceQueryService.GetServicesAsync"/> in that case - "no
/// filter applied" behaves identically to before this filter existed.
/// <see cref="MinPrice"/>/<see cref="MaxPrice"/> compare against
/// <see cref="Domain.Services.Service.Price"/> parsed to a
/// <see cref="decimal"/> via <see cref="ServicePriceParser"/> - Price is a
/// display-only string field (same convention as <c>CustomerDto.LifetimeValue</c>/
/// <c>BookingDto.Price</c>), never stored as a number, so a range
/// comparison has to parse it first. <see cref="IsAssigned"/> - true for
/// "has at least one assigned specialist", false for "has none" - is the
/// one criterion Sprint 5 Commit 2's own Presentation ViewModel does not
/// expose a control for yet (not in this commit's requested property
/// list), kept here anyway since the underlying data
/// (<see cref="Domain.Services.IServiceRepository.GetAssignedSpecialistsAsync"/>)
/// already exists to support it - the same "richer DTO than the current
/// UI wires up" shape <c>CustomerSearchFilter.FullName</c>/<c>Email</c>/<c>Phone</c>
/// already established.
/// </summary>
public sealed record ServiceSearchFilter(
    string? SearchText = null,
    ServiceCategory? Category = null,
    ServiceStatus? Status = null,
    int? MinDurationMinutes = null,
    int? MaxDurationMinutes = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsAssigned = null)
{
    public static ServiceSearchFilter Empty { get; } = new();
}
