namespace Rojan.Desktop.Application.Inventory;

/// <summary>Read-only use case Presentation depends on to load one product's profile (details plus stock level, recent transactions, and linked services) as a single unit of work.</summary>
public interface IProductProfileQueryService
{
    public Task<ProductProfileDto> GetProfileAsync(string productId, CancellationToken cancellationToken = default);
}
