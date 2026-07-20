using Rojan.Desktop.Application.Customers;
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
            MakeCustomer(customerId, "Placeholder"), [], [], [], [])));

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
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubCustomerQueryService(
            _ => Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom")));

        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void SearchText_MatchesNameCompanyOrEmail_FiltersToMatchingCustomersOnly()
    {
        var customers = new List<CustomerDto>
        {
            MakeCustomer("customer-1", "Amelia Hart", company: "Hart & Co. Salon"),
            MakeCustomer("customer-2", "Noah Bennett", email: "noah.bennett@example.com"),
            MakeCustomer("customer-3", "Sophia Reyes", company: "Reyes Beauty Studio"),
        };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.SearchText = "reyes";

        Assert.Equal(["customer-3"], sut.Customers.Select(c => c.Id));
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredCustomer()
    {
        var customers = new List<CustomerDto>
        {
            MakeCustomer("customer-1", "Amelia Hart"),
            MakeCustomer("customer-2", "Noah Bennett"),
        };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());
        sut.SelectedCustomer = customers[0];

        sut.SearchText = "Noah";

        Assert.Equal(customers[1], sut.SelectedCustomer);
    }

    [Fact]
    public void SearchText_NoCustomerMatches_SelectedCustomerBecomesNull()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService, MakeProfileQueryService(), new StubCustomerCommandService());

        sut.SearchText = "no-such-customer";

        Assert.Empty(sut.Customers);
        Assert.Null(sut.SelectedCustomer);
        Assert.Null(sut.Profile);
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
}
