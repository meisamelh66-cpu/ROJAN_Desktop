using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

/// <summary>Configurable <see cref="IProductProfileQueryService"/> test double - same reasoning as Customers.StubCustomerProfileQueryService.</summary>
internal sealed class StubProductProfileQueryService : IProductProfileQueryService
{
    private readonly Func<string, CancellationToken, Task<ProductProfileDto>> _getProfile;

    public StubProductProfileQueryService(Func<string, CancellationToken, Task<ProductProfileDto>> getProfile)
    {
        _getProfile = getProfile;
    }

    public Task<ProductProfileDto> GetProfileAsync(string productId, CancellationToken cancellationToken = default) =>
        _getProfile(productId, cancellationToken);
}
