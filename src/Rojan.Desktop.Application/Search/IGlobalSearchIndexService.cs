namespace Rojan.Desktop.Application.Search;

/// <summary>Phase 28: Enterprise Global Search &amp; Command Palette. Aggregates live business data (Customers/Bookings/Specialists/Services/Products) into a unified, already-plain-text candidate set - the "search customers, services, staff, products, bookings" requirement. Pages/Settings/Commands are a separate, Presentation-owned static catalog (they need localized text, which this Application-layer service cannot see) - <c>ViewModels.Search.CommandPaletteViewModel</c> combines both before ranking.</summary>
public interface IGlobalSearchIndexService
{
    public Task<IReadOnlyList<SearchCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);
}
