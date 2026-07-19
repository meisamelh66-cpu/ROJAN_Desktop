using Rojan.Desktop.Application.Services;
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

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsServices_StateIsLoadedAndPopulatesServices()
    {
        var services = new List<ServiceDto> { MakeService("service-1", "Haircut & Style") };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(services, sut.Services);
        Assert.Equal(services[0], sut.SelectedService);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedService);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubServiceQueryService(
            _ => Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom")));

        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void SearchText_MatchesNameOrCategory_FiltersToMatchingServicesOnly()
    {
        var services = new List<ServiceDto>
        {
            MakeService("service-1", "Haircut & Style", ServiceCategory.Hair),
            MakeService("service-2", "Manicure", ServiceCategory.Nails),
            MakeService("service-3", "Facial Renewal", ServiceCategory.Skin),
        };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());

        sut.SearchText = "nails";

        Assert.Equal(["service-2"], sut.Services.Select(s => s.Id));
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredService()
    {
        var services = new List<ServiceDto>
        {
            MakeService("service-1", "Haircut & Style"),
            MakeService("service-2", "Manicure"),
        };
        var queryService = new StubServiceQueryService(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());
        sut.SelectedService = services[0];

        sut.SearchText = "Manicure";

        Assert.Equal(services[1], sut.SelectedService);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var services = new List<ServiceDto> { MakeService("service-1", "Haircut & Style") };
        var queryService = new StubServiceQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<ServiceDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<ServiceDto>>(services));
        var sut = new ServicePageViewModel(queryService, MakeProfileQueryService(), new StubServiceCommandService());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(services, sut.Services);
    }
}
