using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Configurable <see cref="IServiceProfileQueryService"/> test double - same reasoning as Customers.StubCustomerProfileQueryService.</summary>
internal sealed class StubServiceProfileQueryService : IServiceProfileQueryService
{
    private readonly Func<string, CancellationToken, Task<ServiceProfileDto>> _getProfile;

    public StubServiceProfileQueryService(Func<string, CancellationToken, Task<ServiceProfileDto>> getProfile)
    {
        _getProfile = getProfile;
    }

    public Task<ServiceProfileDto> GetProfileAsync(string serviceId, CancellationToken cancellationToken = default) =>
        _getProfile(serviceId, cancellationToken);
}
