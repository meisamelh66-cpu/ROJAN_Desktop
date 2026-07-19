using Rojan.Desktop.Application.Services;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

public sealed class ServiceQueryServiceTests
{
    [Fact]
    public async Task GetServicesAsync_RepositoryReturnsServices_MapsEveryFieldToDto()
    {
        var domainService = new DomainServices.Service(
            "service-1", "Haircut & Style", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active,
            60, "$65", "Classic cut and blow-dry finish.");
        var repository = new StubServiceRepository([domainService]);
        var sut = new ServiceQueryService(repository);

        var result = await sut.GetServicesAsync();

        var dto = Assert.Single(result);
        Assert.Equal(domainService.Id, dto.Id);
        Assert.Equal(domainService.Name, dto.Name);
        Assert.Equal(ServiceCategory.Hair, dto.Category);
        Assert.Equal(ServiceStatus.Active, dto.Status);
        Assert.Equal(domainService.DurationMinutes, dto.DurationMinutes);
        Assert.Equal(domainService.Price, dto.Price);
        Assert.Equal(domainService.Description, dto.Description);
    }

    [Fact]
    public async Task GetServicesAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubServiceRepository([]);
        var sut = new ServiceQueryService(repository);

        var result = await sut.GetServicesAsync();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DomainServices.ServiceStatus.Active, ServiceStatus.Active)]
    [InlineData(DomainServices.ServiceStatus.Seasonal, ServiceStatus.Seasonal)]
    [InlineData(DomainServices.ServiceStatus.Discontinued, ServiceStatus.Discontinued)]
    public async Task GetServicesAsync_EachDomainStatus_MapsToMatchingApplicationStatus(
        DomainServices.ServiceStatus domainStatus, ServiceStatus expectedStatus)
    {
        var domainService = new DomainServices.Service(
            "service-1", "Test Service", DomainServices.ServiceCategory.Hair, domainStatus, 60, "$0", string.Empty);
        var repository = new StubServiceRepository([domainService]);
        var sut = new ServiceQueryService(repository);

        var result = await sut.GetServicesAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }

    [Theory]
    [InlineData(DomainServices.ServiceCategory.Hair, ServiceCategory.Hair)]
    [InlineData(DomainServices.ServiceCategory.Colour, ServiceCategory.Colour)]
    [InlineData(DomainServices.ServiceCategory.Nails, ServiceCategory.Nails)]
    [InlineData(DomainServices.ServiceCategory.Skin, ServiceCategory.Skin)]
    [InlineData(DomainServices.ServiceCategory.Spa, ServiceCategory.Spa)]
    [InlineData(DomainServices.ServiceCategory.Consultation, ServiceCategory.Consultation)]
    public async Task GetServicesAsync_EachDomainCategory_MapsToMatchingApplicationCategory(
        DomainServices.ServiceCategory domainCategory, ServiceCategory expectedCategory)
    {
        var domainService = new DomainServices.Service(
            "service-1", "Test Service", domainCategory, DomainServices.ServiceStatus.Active, 60, "$0", string.Empty);
        var repository = new StubServiceRepository([domainService]);
        var sut = new ServiceQueryService(repository);

        var result = await sut.GetServicesAsync();

        Assert.Equal(expectedCategory, Assert.Single(result).Category);
    }

    private static IReadOnlyList<DomainServices.Service> MakeSearchFixture() =>
    [
        new("service-1", "Haircut & Style", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active,
            60, "$65", "Classic cut and blow-dry finish."),
        new("service-2", "Manicure", DomainServices.ServiceCategory.Nails, DomainServices.ServiceStatus.Active,
            45, "$40", "Classic manicure with polish."),
        new("service-3", "Facial Renewal", DomainServices.ServiceCategory.Skin, DomainServices.ServiceStatus.Active,
            60, "$85", "Deep-cleansing facial treatment."),
    ];

    [Fact]
    public async Task SearchServicesAsync_TextMatchesName_ReturnsOnlyThatService()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync("manicure");

        Assert.Equal("service-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_TextMatchesCategory_ReturnsOnlyThatService()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync("skin");

        Assert.Equal("service-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_EmptySearchText_ReturnsEveryService()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(string.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchServicesAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync("no-such-service");

        Assert.Empty(result);
    }
}
