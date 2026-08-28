using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

public sealed class ServicePageViewModelTests
{
    private static ServiceDto MakeService(string id, string name, ServiceCategory category = ServiceCategory.Hair) =>
        new(id, name, category, ServiceStatus.Active, 60, "$0", string.Empty);

    /// <summary>A profile query stub that never fails, used by tests that don't assert on Profile - Profile is constructed as a side effect of selection, and its own errors are contained internally.</summary>
    private static StubServiceProfileQueryService MakeProfileQueryService() =>
        new((serviceId, _) => Task.FromResult(new ServiceProfileDto(
            MakeService(serviceId, "Placeholder"), [])));

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ServiceDto>>();
        var queryService = new StubServiceQueryService(_ => tcs.Task);

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsServices_StateIsLoadedAndPopulatesServices()
    {
        var services = new List<ServiceDto> { MakeService("service-1", "Haircut & Style") };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(services, sut.Services);
        Assert.Equal(services[0], sut.SelectedService);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedService);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubServiceQueryService(
            _ => Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom")));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 8.19 Logging Wave 2A: LoadAsync / LoadCategoriesAsync / CreateServiceAsync now log at
    // Error before their existing handling - user-visible behaviour unchanged.

    [Fact]
    public void LoadAsync_QueryServiceThrows_LogsError()
    {
        var queryService = new StubServiceQueryService(
            _ => Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<ServicePageViewModel>();

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine(), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubServiceQueryService(
            _ => Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() =>
            new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine()));

        Assert.Null(exception);
    }

    // Sprint 5 Commit 2: premium service search and filters. Text/field-matching behavior now
    // lives in ServiceQueryService (see ServiceQueryServiceTests), so these ViewModel tests assert
    // on filter composition (what got asked for) - same split Customers/Bookings' own Commit 2
    // passes established.

    [Fact]
    public void Constructor_NoFilterApplied_SearchesWithAnAllDefaultFilter()
    {
        // "Keep existing service list behavior unchanged when no filter is applied" - an
        // all-default ServiceSearchFilter is documented to behave identically to the old
        // unfiltered GetServicesAsync call (see ServiceSearchFilter's own doc comment).
        var services = new List<ServiceDto> { MakeService("service-1", "Haircut & Style") };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        var filter = Assert.Single(queryService.SearchCalls);
        Assert.Null(filter.SearchText);
        Assert.Null(filter.Category);
        Assert.Null(filter.Status);
        Assert.Null(filter.MinDurationMinutes);
        Assert.Null(filter.MaxDurationMinutes);
        Assert.Null(filter.MinPrice);
        Assert.Null(filter.MaxPrice);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(services, sut.Services);
    }

    [Fact]
    public void SearchText_Changed_SearchesWithSearchTextInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.SearchText = "manicure";

        Assert.Equal("manicure", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void SelectedCategory_Changed_SearchesWithCategoryInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.SelectedCategory = ServiceCategory.Nails;

        Assert.Equal(ServiceCategory.Nails, queryService.SearchCalls[^1].Category);
    }

    [Fact]
    public void SelectedStatus_Changed_SearchesWithStatusInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.SelectedStatus = ServiceStatus.Seasonal;

        Assert.Equal(ServiceStatus.Seasonal, queryService.SearchCalls[^1].Status);
    }

    [Fact]
    public void MinDuration_Changed_SearchesWithMinDurationInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.MinDuration = 30;

        Assert.Equal(30, queryService.SearchCalls[^1].MinDurationMinutes);
    }

    [Fact]
    public void MaxDuration_Changed_SearchesWithMaxDurationInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.MaxDuration = 90;

        Assert.Equal(90, queryService.SearchCalls[^1].MaxDurationMinutes);
    }

    [Fact]
    public void MinPrice_Changed_SearchesWithMinPriceInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.MinPrice = 25m;

        Assert.Equal(25m, queryService.SearchCalls[^1].MinPrice);
    }

    [Fact]
    public void MaxPrice_Changed_SearchesWithMaxPriceInFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        sut.MaxPrice = 100m;

        Assert.Equal(100m, queryService.SearchCalls[^1].MaxPrice);
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredService()
    {
        var services = new List<ServiceDto>
        {
            MakeService("service-1", "Haircut & Style"),
            MakeService("service-2", "Manicure"),
        };
        var queryService = new StubServiceQueryService(
            _ => Task.FromResult<IReadOnlyList<ServiceDto>>(services),
            searchServicesByFilter: (filter, _) => Task.FromResult<IReadOnlyList<ServiceDto>>(
                string.IsNullOrEmpty(filter.SearchText)
                    ? services
                    : services.Where(service => service.Name.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)).ToList()));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());
        sut.SelectedService = services[0];

        sut.SearchText = "Manicure";

        Assert.Equal(services[1], sut.SelectedService);
    }

    [Fact]
    public void SearchCommand_Executed_ReRunsSearchWithCurrentFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine())
        {
            SearchText = "manicure",
        };
        var callsBeforeExplicitSearch = queryService.SearchCalls.Count;

        sut.SearchCommand.Execute(null);

        Assert.True(queryService.SearchCalls.Count > callsBeforeExplicitSearch);
        Assert.Equal("manicure", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void ClearFiltersCommand_Executed_ResetsEveryFilterAndReloadsWithDefaultFilter()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine())
        {
            SearchText = "manicure",
            SelectedCategory = ServiceCategory.Nails,
            SelectedStatus = ServiceStatus.Active,
            MinDuration = 30,
            MaxDuration = 90,
            MinPrice = 10m,
            MaxPrice = 200m,
        };

        sut.ClearFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SearchText);
        Assert.Null(sut.SelectedCategory);
        Assert.Null(sut.SelectedStatus);
        Assert.Null(sut.MinDuration);
        Assert.Null(sut.MaxDuration);
        Assert.Null(sut.MinPrice);
        Assert.Null(sut.MaxPrice);

        var filter = queryService.SearchCalls[^1];
        Assert.Null(filter.SearchText);
        Assert.Null(filter.Category);
        Assert.Null(filter.Status);
        Assert.Null(filter.MinDurationMinutes);
        Assert.Null(filter.MaxDurationMinutes);
        Assert.Null(filter.MinPrice);
        Assert.Null(filter.MaxPrice);
    }

    [Fact]
    public void ResultCount_AfterLoad_MatchesServicesCount()
    {
        var services = new List<ServiceDto>
        {
            MakeService("service-1", "Haircut & Style"),
            MakeService("service-2", "Manicure"),
        };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(2, sut.ResultCount);
        Assert.Equal(sut.Services.Count, sut.ResultCount);
    }

    [Fact]
    public void ResultCount_NoMatches_IsZero()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(0, sut.ResultCount);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var services = new List<ServiceDto> { MakeService("service-1", "Haircut & Style") };
        var queryService = new StubServiceQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<ServiceDto>>(services));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService(), new StubIntelligenceEngine());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(services, sut.Services);
    }
}
