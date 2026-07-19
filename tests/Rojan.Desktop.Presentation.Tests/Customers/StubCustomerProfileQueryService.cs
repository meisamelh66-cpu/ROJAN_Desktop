using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Presentation.Tests.Customers;

/// <summary>Configurable <see cref="ICustomerProfileQueryService"/> test double - same reasoning as StubCustomerQueryService.</summary>
internal sealed class StubCustomerProfileQueryService : ICustomerProfileQueryService
{
    private readonly Func<string, CancellationToken, Task<CustomerProfileDto>> _getProfile;

    public StubCustomerProfileQueryService(Func<string, CancellationToken, Task<CustomerProfileDto>> getProfile)
    {
        _getProfile = getProfile;
    }

    public Task<CustomerProfileDto> GetProfileAsync(string customerId, CancellationToken cancellationToken = default) =>
        _getProfile(customerId, cancellationToken);
}
