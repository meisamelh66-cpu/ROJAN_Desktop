using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

/// <summary>Records whether <see cref="Invalidate"/> was called - lets SalonCommandServiceTests assert the cache-invalidation contract without a real BackendSalonContextService.</summary>
internal sealed class StubSalonContextService : ISalonContextService
{
    public int InvalidateCallCount { get; private set; }

    public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

    public void Invalidate() => InvalidateCallCount++;
}
