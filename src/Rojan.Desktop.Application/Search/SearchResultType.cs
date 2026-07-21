namespace Rojan.Desktop.Application.Search;

/// <summary>Phase 28: Enterprise Global Search &amp; Command Palette. Every kind of thing the palette can surface - pages/modules (which already covers Settings, itself just another module), the five live-business-data verticals the spec names, and executable commands.</summary>
public enum SearchResultType
{
    Page,
    Customer,
    Booking,
    Specialist,
    Service,
    Product,
    Command,
}
