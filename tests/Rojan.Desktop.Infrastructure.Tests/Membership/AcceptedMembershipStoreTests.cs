using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Membership;
using Rojan.Desktop.Infrastructure.Membership;

namespace Rojan.Desktop.Infrastructure.Tests.Membership;

/// <summary>
/// Exercises <see cref="AcceptedMembershipStore"/> - JSON round-trip through
/// an in-memory <see cref="ISecureStorageService"/> stub (the real DPAPI
/// implementation is exercised separately, same convention as
/// <c>BackendSessionServiceTests</c>).
/// </summary>
public sealed class AcceptedMembershipStoreTests
{
    [Fact]
    public async Task GetAsync_NothingPersistedYet_ReturnsNull()
    {
        var store = new AcceptedMembershipStore(new StubSecureStorageService());

        var result = await store.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsTheMembership()
    {
        var storage = new StubSecureStorageService();
        var store = new AcceptedMembershipStore(storage);
        var membership = new AcceptedMembership("salon-1", "Glow Salon", "RECEPTIONIST");

        await store.SaveAsync(membership);
        var result = await store.GetAsync();

        Assert.Equal(membership, result);
        Assert.True(storage.ContainsKey("membership:accepted"));
    }

    [Fact]
    public async Task ClearAsync_RemovesThePersistedMembership()
    {
        var storage = new StubSecureStorageService();
        var store = new AcceptedMembershipStore(storage);
        await store.SaveAsync(new AcceptedMembership("salon-1", "Glow Salon", "RECEPTIONIST"));

        await store.ClearAsync();

        Assert.Null(await store.GetAsync());
        Assert.False(storage.ContainsKey("membership:accepted"));
    }

    [Fact]
    public async Task GetAsync_CorruptData_TreatedAsNotPresent()
    {
        var storage = new StubSecureStorageService();
        await storage.SetAsync("membership:accepted", "{ not valid json");
        var store = new AcceptedMembershipStore(storage);

        var result = await store.GetAsync();

        Assert.Null(result);
    }

    /// <summary>In-memory stand-in for <see cref="ISecureStorageService"/> - same shape as <c>BackendSessionServiceTests.StubSecureStorageService</c>, redefined locally per this test project's own "self-contained stub per file" convention.</summary>
    private sealed class StubSecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _store = [];

        public bool ContainsKey(string key) => _store.ContainsKey(key);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.GetValueOrDefault(key));

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}
