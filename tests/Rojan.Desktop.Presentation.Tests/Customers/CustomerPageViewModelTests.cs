using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Customers;

public sealed class CustomerPageViewModelTests
{
    private static CustomerDto MakeCustomer(string id, string fullName, string company = "", string email = "") =>
        new(id, fullName, company, email, string.Empty, CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1");

    /// <summary>A profile query stub that never fails, used by tests that don't assert on Profile - Profile is constructed as a side effect of selection, and its own errors are contained internally (CustomerProfileViewModel catches them itself).</summary>
    private static StubCustomerProfileQueryService MakeProfileQueryService() =>
        new((customerId, _) => Task.FromResult(new CustomerProfileDto(
            MakeCustomer(customerId, "Placeholder"), [], [], [], [], CustomerBookingSummaryDto.Empty, CustomerInsightsDto.Empty)));

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<CustomerDto>>();
        var queryService = new StubCustomerQueryService(_ => tcs.Task);

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsCustomers_StateIsLoadedAndPopulatesCustomers()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(customers, sut.Customers);
        Assert.Equal(customers[0], sut.SelectedCustomer);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedCustomer);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsGenericErrorMessage()
    {
        var queryService = new StubCustomerQueryService(
            _ => Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom for customer Amelia Hart")));

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain("Amelia Hart", sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    // Phase 8.19 Logging Wave 2A: LoadAsync logs at Error before surfacing the Error state.
    // Phase 8.108 P2 sub-wave 2: the Error-state ErrorMessage is now the generic
    // Strings.Common_ActionFailedMessage, never the raw exception message (which can carry customer PII).

    [Fact]
    public void LoadAsync_QueryServiceThrows_LogsError_AndSurfacesGenericMessage()
    {
        var queryService = new StubCustomerQueryService(
            _ => Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom for customer Amelia Hart")));
        var logger = new RecordingLogger<CustomerPageViewModel>();

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService(), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain("Amelia Hart", sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubCustomerQueryService(
            _ => Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() =>
            new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService()));

        Assert.Null(exception);
    }

    // Sprint 4 Commit 2: customer search and filters. SearchText/CompanyFilter/TagFilter/
    // StatusFilter are combined into one CustomerSearchFilter and run through
    // ICustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, ...) - actual text/field
    // matching behavior now lives in CustomerQueryService (see CustomerQueryServiceTests), so these
    // ViewModel tests assert on filter composition (what got asked for), same split Bookings uses.

    [Fact]
    public void Constructor_NoFilterApplied_SearchesWithAnAllDefaultFilter()
    {
        // "Keep existing customer list behavior unchanged when no filter is applied" - an
        // all-default CustomerSearchFilter is documented to behave identically to the old
        // unfiltered GetCustomersAsync call (see CustomerSearchFilter's own doc comment).
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        var filter = Assert.Single(queryService.SearchCalls);
        Assert.Null(filter.SearchText);
        Assert.Null(filter.Company);
        Assert.Null(filter.Status);
        Assert.Null(filter.Tag);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(customers, sut.Customers);
    }

    [Fact]
    public void SearchText_Changed_SearchesWithSearchTextInFilter()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.SearchText = "reyes";

        Assert.Equal("reyes", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void CompanyFilter_Changed_SearchesWithCompanyInFilter()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.CompanyFilter = "Hart & Co. Salon";

        Assert.Equal("Hart & Co. Salon", queryService.SearchCalls[^1].Company);
    }

    [Fact]
    public void TagFilter_Changed_SearchesWithTagInFilter()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.TagFilter = "VIP";

        Assert.Equal("VIP", queryService.SearchCalls[^1].Tag);
    }

    [Fact]
    public void StatusFilter_Changed_SearchesWithStatusInFilter()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.StatusFilter = CustomerStatus.Vip;

        Assert.Equal(CustomerStatus.Vip, queryService.SearchCalls[^1].Status);
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredCustomer()
    {
        var customers = new List<CustomerDto>
        {
            MakeCustomer("customer-1", "Amelia Hart"),
            MakeCustomer("customer-2", "Noah Bennett"),
        };
        var queryService = new StubCustomerQueryService(
            _ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers),
            searchCustomersByFilter: (filter, _) => Task.FromResult<IReadOnlyList<CustomerDto>>(
                string.IsNullOrEmpty(filter.SearchText)
                    ? customers
                    : customers.Where(c => c.FullName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)).ToList()));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());
        sut.SelectedCustomer = customers[0];

        sut.SearchText = "Noah";

        Assert.Equal(customers[1], sut.SelectedCustomer);
    }

    [Fact]
    public void SearchText_NoCustomerMatches_SelectedCustomerBecomesNull()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(
            _ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers),
            searchCustomersByFilter: (filter, _) => Task.FromResult<IReadOnlyList<CustomerDto>>(
                string.IsNullOrEmpty(filter.SearchText)
                    ? customers
                    : customers.Where(c => c.FullName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)).ToList()));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.SearchText = "no-such-customer";

        Assert.Empty(sut.Customers);
        Assert.Null(sut.SelectedCustomer);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void CreateCustomerCommand_Executed_ReloadPreservesActiveFilter()
    {
        // A create action's reload must not silently drop the user's active filter - same
        // "filter survives a mutating action" behavior Bookings.BookingPageViewModel established.
        var existing = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(existing.ToList()));
        var commandService = new StubCustomerCommandService
        {
            OnCustomerCreated = (_, dto) => existing.Add(dto),
        };
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), commandService)
        {
            NewCustomerFullName = "Grace Kim",
        };
        sut.CompanyFilter = "Hart & Co. Salon";

        sut.CreateCustomerCommand.Execute(null);

        Assert.Equal("Hart & Co. Salon", queryService.SearchCalls[^1].Company);
    }

    [Fact]
    public void LoadAsync_OlderSearchCompletesAfterNewer_OlderResultIsDiscarded()
    {
        // Stale search result protection: if the user changes the filter again before the first
        // search's response arrives, the slower/older response must not overwrite the newer one -
        // same _filterVersion guard as Bookings.BookingPageViewModel.
        // Index 0 is the constructor's own initial load (never resolved - left pending, harmless).
        // Index 1 is triggered by SearchText = "first" (the older, slower search).
        // Index 2 is triggered by SearchText = "second" (the newer search, whose result must win).
        var completionSources = new List<TaskCompletionSource<IReadOnlyList<CustomerDto>>>
        {
            new(), new(), new(),
        };
        var callCount = 0;
        var queryService = new StubCustomerQueryService(
            _ => Task.FromResult<IReadOnlyList<CustomerDto>>([]),
            searchCustomersByFilter: (_, _) => completionSources[callCount++].Task);
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.SearchText = "first";
        sut.SearchText = "second";

        var newerResult = new List<CustomerDto> { MakeCustomer("customer-new", "Second Result") };
        completionSources[2].SetResult(newerResult);
        var olderResult = new List<CustomerDto> { MakeCustomer("customer-old", "First Result") };
        completionSources[1].SetResult(olderResult);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(newerResult, sut.Customers);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(customers, sut.Customers);
    }

    [Fact]
    public void SelectedCustomer_SetToNull_ClearsProfile()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());
        Assert.NotNull(sut.Profile);

        sut.SelectedCustomer = null;

        Assert.Null(sut.Profile);
    }

    [Fact]
    public void CreateCustomerCommand_FullNameIsEmpty_CanExecuteIsFalse()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.False(sut.CreateCustomerCommand.CanExecute(null));

        sut.NewCustomerFullName = "Grace Kim";

        Assert.True(sut.CreateCustomerCommand.CanExecute(null));
    }

    [Fact]
    public void CreateCustomerCommand_Executed_CallsCommandServiceReloadsListAndSelectsNewCustomer()
    {
        var existing = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(existing.ToList()));
        var commandService = new StubCustomerCommandService
        {
            OnCustomerCreated = (_, dto) => existing.Add(dto),
        };
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), commandService)
        {
            NewCustomerFullName = "Grace Kim",
            NewCustomerCompany = "Kim Aesthetics",
            NewCustomerEmail = "grace.kim@example.com",
            NewCustomerPhone = "555-0199",
        };

        sut.CreateCustomerCommand.Execute(null);

        var request = Assert.Single(commandService.CreateRequests);
        Assert.Equal("Grace Kim", request.FullName);
        Assert.Equal("Kim Aesthetics", request.Company);
        Assert.Equal(string.Empty, sut.NewCustomerFullName);
        Assert.Equal("new-customer", sut.SelectedCustomer?.Id);
    }

    // ---- Phase 8.66: Production Hardening (missing-guard sweep, Wave A) ----

    [Fact]
    public void CreateCustomerCommand_BackendThrows_SetsInlineCreateError_DoesNotThrow_PreservesForm_LogsOperationOnly()
    {
        const string backendBody = "HTTP 500: backend response body / customer PII secret";
        var existing = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(existing.ToList()));
        var commandService = new StubCustomerCommandService { CreateCustomerException = new InvalidOperationException(backendBody) };
        var logger = new RecordingLogger<CustomerPageViewModel>();
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), commandService, logger: logger)
        {
            NewCustomerFullName = "Grace Kim",
            NewCustomerEmail = "grace.kim@example.com",
        };

        var exception = Record.Exception(() => sut.CreateCustomerCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasCreateError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.CreateErrorMessage);
        Assert.Equal("Grace Kim", sut.NewCustomerFullName); // form preserved for retry
        Assert.Equal("grace.kim@example.com", sut.NewCustomerEmail);
        Assert.NotEqual(DashboardState.Error, sut.State); // page not replaced with the full error view
        var entry = Assert.Single(logger.Entries.FindAll(e => e.Message.Contains("CreateCustomerAsync", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.DoesNotContain(backendBody, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateCustomerCommand_Succeeds_ClearsAnyPriorInlineCreateError()
    {
        var existing = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(existing.ToList()));
        var commandService = new StubCustomerCommandService
        {
            CreateCustomerException = new InvalidOperationException("boom"),
            OnCustomerCreated = (_, dto) => existing.Add(dto),
        };
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), commandService) { NewCustomerFullName = "Grace Kim" };
        sut.CreateCustomerCommand.Execute(null);
        Assert.True(sut.HasCreateError);

        commandService.CreateCustomerException = null;
        sut.NewCustomerFullName = "Grace Kim";
        sut.CreateCustomerCommand.Execute(null);

        Assert.False(sut.HasCreateError);
        Assert.Null(sut.CreateErrorMessage);
    }

    [Fact]
    public void LoggerFactory_ForwardedToProfileChild_ChildLoadFailureIsLoggedViaTheFactory()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var failingProfileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromException<CustomerProfileDto>(new InvalidOperationException("child boom")));
        var loggerFactory = new RecordingLoggerFactory();

        var sut = new CustomerPageViewModel(queryService, failingProfileQuery, new StubCustomerCommandService(), logger: null, loggerFactory: loggerFactory);

        Assert.NotNull(sut.Profile);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(CustomerProfileViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("child boom", entry.Message, StringComparison.Ordinal);
    }
}
