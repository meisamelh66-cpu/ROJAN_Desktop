using Rojan.Desktop.Application.Salons;
using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

public sealed class SalonQueryServiceTests
{
    [Fact]
    public async Task GetMySalonAsync_RepositoryReturnsOneSalon_MapsEveryField()
    {
        var domainSalon = new DomainSalons.Salon(
            "salon-1", "Glow Salon", "Full-service hair and beauty salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", true);
        var repository = new StubSalonRepository([domainSalon]);
        var sut = new SalonQueryService(repository);

        var result = await sut.GetMySalonAsync();

        Assert.NotNull(result);
        Assert.Equal(domainSalon.Id, result!.Id);
        Assert.Equal(domainSalon.Name, result.Name);
        Assert.Equal(domainSalon.Description, result.Description);
        Assert.Equal(domainSalon.Phone, result.Phone);
        Assert.Equal(domainSalon.Email, result.Email);
        Assert.Equal(domainSalon.Address, result.Address);
        Assert.Equal(domainSalon.Active, result.Active);
    }

    [Fact]
    public async Task GetMySalonAsync_RepositoryReturnsNoSalons_ReturnsNull()
    {
        var repository = new StubSalonRepository([]);
        var sut = new SalonQueryService(repository);

        var result = await sut.GetMySalonAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMySalonAsync_RepositoryReturnsMultipleSalons_ReturnsTheFirstOne()
    {
        var first = new DomainSalons.Salon("salon-1", "First Salon", null, "111", null, "Addr 1", true);
        var second = new DomainSalons.Salon("salon-2", "Second Salon", null, "222", null, "Addr 2", true);
        var repository = new StubSalonRepository([first, second]);
        var sut = new SalonQueryService(repository);

        var result = await sut.GetMySalonAsync();

        Assert.Equal("salon-1", result!.Id);
    }

    [Fact]
    public async Task GetMySalonAsync_NullDescriptionAndEmail_MapToEmptyString()
    {
        var domainSalon = new DomainSalons.Salon("salon-1", "Glow Salon", null, "+1 555 0100", null, "1 Main St", true);
        var repository = new StubSalonRepository([domainSalon]);
        var sut = new SalonQueryService(repository);

        var result = await sut.GetMySalonAsync();

        Assert.Equal(string.Empty, result!.Description);
        Assert.Equal(string.Empty, result.Email);
    }
}
