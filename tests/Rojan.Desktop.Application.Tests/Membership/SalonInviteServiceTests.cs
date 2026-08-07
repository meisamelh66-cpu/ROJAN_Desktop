using Rojan.Desktop.Application.Membership;
using DomainMembership = Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Application.Tests.Membership;

public sealed class SalonInviteServiceTests
{
    [Fact]
    public async Task GetDetailsAsync_MapsSalonNameAndRole()
    {
        var repository = new StubSalonInviteRepository { DetailsResult = new DomainMembership.SalonInviteDetails("Glow Salon", "RECEPTIONIST") };
        var sut = new SalonInviteService(repository, new StubAcceptedMembershipStore());

        var result = await sut.GetDetailsAsync("tok-123");

        Assert.Equal("Glow Salon", result.SalonName);
        Assert.Equal("RECEPTIONIST", result.Role);
    }

    [Fact]
    public async Task AcceptAsync_MapsTheAcceptedMembership()
    {
        var repository = new StubSalonInviteRepository { AcceptResult = new DomainMembership.AcceptedMembership("salon-1", "Glow Salon", "RECEPTIONIST") };
        var sut = new SalonInviteService(repository, new StubAcceptedMembershipStore());

        var result = await sut.AcceptAsync("tok-123", "Glow Salon");

        Assert.Equal("salon-1", result.SalonId);
        Assert.Equal("Glow Salon", result.SalonName);
        Assert.Equal("RECEPTIONIST", result.Role);
    }

    [Fact]
    public async Task AcceptAsync_PersistsTheMembershipViaTheAcceptedMembershipStore()
    {
        var repository = new StubSalonInviteRepository { AcceptResult = new DomainMembership.AcceptedMembership("salon-1", "Glow Salon", "RECEPTIONIST") };
        var membershipStore = new StubAcceptedMembershipStore();
        var sut = new SalonInviteService(repository, membershipStore);

        await sut.AcceptAsync("tok-123", "Glow Salon");

        Assert.NotNull(membershipStore.Saved);
        Assert.Equal("salon-1", membershipStore.Saved!.SalonId);
    }

    [Fact]
    public async Task AcceptAsync_ForwardsTheTokenAndCallerSuppliedSalonNameToTheRepository()
    {
        var repository = new StubSalonInviteRepository { AcceptResult = new DomainMembership.AcceptedMembership("salon-1", "Glow Salon", "RECEPTIONIST") };
        var sut = new SalonInviteService(repository, new StubAcceptedMembershipStore());

        await sut.AcceptAsync("tok-123", "Glow Salon");

        Assert.Equal("tok-123", repository.LastAcceptToken);
        Assert.Equal("Glow Salon", repository.LastAcceptSalonName);
    }

    private sealed class StubSalonInviteRepository : DomainMembership.ISalonInviteRepository
    {
        public DomainMembership.SalonInviteDetails? DetailsResult { get; set; }

        public DomainMembership.AcceptedMembership? AcceptResult { get; set; }

        public string? LastAcceptToken { get; private set; }

        public string? LastAcceptSalonName { get; private set; }

        public Task<DomainMembership.SalonInviteDetails> GetDetailsAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(DetailsResult!);

        public Task<DomainMembership.AcceptedMembership> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default)
        {
            LastAcceptToken = token;
            LastAcceptSalonName = salonName;
            return Task.FromResult(AcceptResult!);
        }
    }

    private sealed class StubAcceptedMembershipStore : IAcceptedMembershipStore
    {
        public DomainMembership.AcceptedMembership? Saved { get; private set; }

        public Task<DomainMembership.AcceptedMembership?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Saved);

        public Task SaveAsync(DomainMembership.AcceptedMembership membership, CancellationToken cancellationToken = default)
        {
            Saved = membership;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Saved = null;
            return Task.CompletedTask;
        }
    }
}
