namespace Rojan.Desktop.Application.Customers;

/// <summary>Everything the Customer 360 profile screen needs for one customer - the full aggregate fetched together as a single unit of work, same reasoning as Dashboard.DashboardOverviewDto.</summary>
public sealed record CustomerProfileDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerNoteDto> Notes,
    IReadOnlyList<CustomerTagDto> Tags,
    IReadOnlyList<CustomerActivityDto> Activity,
    IReadOnlyList<CustomerStatDto> Statistics);
