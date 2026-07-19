using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Configurable <see cref="ISpecialistProfileQueryService"/> test double - same reasoning as Customers.StubCustomerProfileQueryService.</summary>
internal sealed class StubSpecialistProfileQueryService : ISpecialistProfileQueryService
{
    private readonly Func<string, CancellationToken, Task<SpecialistProfileDto>> _getProfile;

    public StubSpecialistProfileQueryService(Func<string, CancellationToken, Task<SpecialistProfileDto>> getProfile)
    {
        _getProfile = getProfile;
    }

    public Task<SpecialistProfileDto> GetProfileAsync(string specialistId, CancellationToken cancellationToken = default) =>
        _getProfile(specialistId, cancellationToken);
}
