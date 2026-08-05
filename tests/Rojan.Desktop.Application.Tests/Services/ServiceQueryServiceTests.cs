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
    [InlineData(DomainServices.ServiceCategory.Other, ServiceCategory.Other)]
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

    [Fact]
    public async Task GetServicesAsync_CategoryNameIsNullForLocalData_PresentWhenSupplied()
    {
        // Reception Booking Integration Phase 1: CategoryName is only ever populated for
        // backend-sourced data (BackendServiceRepository) - local/EF services always pass null through unchanged.
        var localService = new DomainServices.Service(
            "service-1", "Test Service", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active, 60, "$0", string.Empty);
        var backendSourcedService = localService with { Id = "service-2", CategoryName = "Hair" };
        var repository = new StubServiceRepository([localService, backendSourcedService]);
        var sut = new ServiceQueryService(repository);

        var result = await sut.GetServicesAsync();

        Assert.Null(result.Single(dto => dto.Id == "service-1").CategoryName);
        Assert.Equal("Hair", result.Single(dto => dto.Id == "service-2").CategoryName);
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

    // Sprint 5 Commit 2: premium service search and filters.

    [Fact]
    public async Task SearchServicesAsync_EmptyFilter_ReturnsEveryServiceSameAsGetServicesAsync()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var filtered = await sut.SearchServicesAsync(ServiceSearchFilter.Empty);
        var unfiltered = await sut.GetServicesAsync();

        Assert.Equal(unfiltered.Select(service => service.Id), filtered.Select(service => service.Id));
    }

    [Fact]
    public async Task SearchServicesAsync_NullFilterFields_BehaveAsNoFilterApplied()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(
            SearchText: null, Category: null, Status: null,
            MinDurationMinutes: null, MaxDurationMinutes: null,
            MinPrice: null, MaxPrice: null, IsAssigned: null));

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterSearchText_MatchesNameOnly()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(SearchText: "manicure"));

        Assert.Equal("service-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterSearchText_MatchesDescription()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(SearchText: "deep-cleansing"));

        Assert.Equal("service-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterSearchText_DoesNotMatchCategoryName()
    {
        // Deliberate behavior difference from the legacy SearchServicesAsync(string) overload above:
        // Category is now its own first-class filter, so free text no longer fuzzy-matches it.
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(SearchText: "skin"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterCategory_ReturnsOnlyThatCategory()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(Category: ServiceCategory.Nails));

        Assert.Equal("service-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterStatus_ReturnsOnlyThatStatus()
    {
        var services = new List<DomainServices.Service>(MakeSearchFixture())
        {
            new("service-4", "Retired Perm", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Discontinued, 90, "$50", string.Empty),
        };
        var repository = new StubServiceRepository(services);
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(Status: ServiceStatus.Discontinued));

        Assert.Equal("service-4", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterMinDuration_ExcludesShorterServices()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        // Fixture durations: service-1=60, service-2=45, service-3=60.
        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MinDurationMinutes: 60));

        Assert.Equal(["service-1", "service-3"], result.Select(service => service.Id));
    }

    [Fact]
    public async Task SearchServicesAsync_FilterMaxDuration_ExcludesLongerServices()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MaxDurationMinutes: 45));

        Assert.Equal("service-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterDurationRange_IsInclusiveOnBothEnds()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        // Fixture durations: 60, 45, 60 - a [45, 60] range must include all three exactly-boundary values.
        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MinDurationMinutes: 45, MaxDurationMinutes: 60));

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterMinPrice_ExcludesCheaperServices()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        // Fixture prices: service-1=$65, service-2=$40, service-3=$85.
        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MinPrice: 65m));

        Assert.Equal(["service-1", "service-3"], result.Select(service => service.Id));
    }

    [Fact]
    public async Task SearchServicesAsync_FilterMaxPrice_ExcludesPricierServices()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MaxPrice: 40m));

        Assert.Equal("service-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterPriceRange_IsInclusiveOnBothEnds()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MinPrice: 40m, MaxPrice: 85m));

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchServicesAsync_FreeService_ParsesAsZeroPriceForRangeFiltering()
    {
        var services = new List<DomainServices.Service>(MakeSearchFixture())
        {
            new("service-5", "Consultation", DomainServices.ServiceCategory.Consultation, DomainServices.ServiceStatus.Active, 30, "رایگان", string.Empty),
        };
        var repository = new StubServiceRepository(services);
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MaxPrice: 0m));

        Assert.Equal("service-5", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_CombinedFilters_AreAndedTogether()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        // service-2 is Nails/$40/45min - Nails category plus a price floor above $40 must match nobody.
        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(Category: ServiceCategory.Nails, MinPrice: 50m));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchServicesAsync_CombinedFilters_MatchWhenEveryCriterionAgrees()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(
            Category: ServiceCategory.Hair, Status: ServiceStatus.Active, MinDurationMinutes: 60, MaxPrice: 70m));

        Assert.Equal("service-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterMatchesNoService_ReturnsEmptyList()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(SearchText: "no-such-service"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterIsAssignedTrue_ReturnsOnlyServicesWithAssignments()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Alex Stylist"));
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(IsAssigned: true));

        Assert.Equal("service-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilterIsAssignedFalse_ReturnsOnlyServicesWithoutAssignments()
    {
        var repository = new StubServiceRepository(MakeSearchFixture());
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Alex Stylist"));
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(IsAssigned: false));

        Assert.Equal(["service-2", "service-3"], result.Select(service => service.Id));
    }

    [Fact]
    public async Task SearchServicesAsync_PersianSearchText_MatchesCaseInsensitivelyRegardlessOfCulture()
    {
        // Culture-safe search: OrdinalIgnoreCase (same convention Customers/Bookings search already uses)
        // must work correctly against non-Latin script, not just ASCII.
        var services = new List<DomainServices.Service>
        {
            new("service-1", "کوتاهی و استایل مو", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active, 60, "$65", string.Empty),
        };
        var repository = new StubServiceRepository(services);
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(SearchText: "استایل"));

        Assert.Equal("service-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchServicesAsync_FilteredResults_PreserveRepositoryOrder()
    {
        // Deterministic, stable ordering: filtering must never reorder the underlying repository
        // sequence, only narrow it.
        var repository = new StubServiceRepository(MakeSearchFixture());
        var sut = new ServiceQueryService(repository);

        var result = await sut.SearchServicesAsync(new ServiceSearchFilter(MaxPrice: 100m));

        Assert.Equal(["service-1", "service-2", "service-3"], result.Select(service => service.Id));
    }
}
