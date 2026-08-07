using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Presentation.Tests.Salons;

/// <summary>Configurable <see cref="ISalonQueryService"/> test double - same reasoning as Services.StubServiceQueryService.</summary>
internal sealed class StubSalonQueryService(Func<CancellationToken, Task<SalonDto?>> getMySalon) : ISalonQueryService
{
    public Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default) => getMySalon(cancellationToken);
}
