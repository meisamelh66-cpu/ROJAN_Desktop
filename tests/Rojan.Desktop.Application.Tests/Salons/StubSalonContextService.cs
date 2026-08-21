using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

/// <summary>Records whether <see cref="Invalidate"/> was called - lets SalonCommandServiceTests assert the cache-invalidation contract without a real BackendSalonContextService.</summary>
internal sealed class StubSalonContextService : ISalonContextService
{
    public int InvalidateCallCount { get; private set; }

    /// <summary>QR Ecosystem (Desktop Productionization Sprint 1): settable so <c>SalonQueryServiceTests</c> can exercise <see cref="ISalonQueryService.GetSalonQrCodeAsync"/>'s salon-resolution step - defaults to <see langword="null"/>, preserving every existing test's "no real salon" assumption unchanged.</summary>
    public string? SalonId { get; set; }

    public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(SalonId);

    public void Invalidate() => InvalidateCallCount++;
}
