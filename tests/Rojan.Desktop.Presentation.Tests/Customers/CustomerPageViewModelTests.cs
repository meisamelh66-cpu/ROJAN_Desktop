using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Customers;

public sealed class CustomerPageViewModelTests
{
    private static CustomerDto MakeCustomer(string id, string fullName, string company = "", string email = "") =>
        new(id, fullName, company, email, string.Empty, CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty);

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<CustomerDto>>();
        var queryService = new StubCustomerQueryService(_ => tcs.Task);

        var sut = new CustomerPageViewModel(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsCustomers_StateIsLoadedAndPopulatesCustomers()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));

        var sut = new CustomerPageViewModel(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(customers, sut.Customers);
        Assert.Equal(customers[0], sut.SelectedCustomer);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>([]));

        var sut = new CustomerPageViewModel(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedCustomer);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubCustomerQueryService(
            _ => Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom")));

        var sut = new CustomerPageViewModel(queryService);

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
        var sut = new CustomerPageViewModel(queryService);

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
        var sut = new CustomerPageViewModel(queryService);
        sut.SelectedCustomer = customers[0];

        sut.SearchText = "Noah";

        Assert.Equal(customers[1], sut.SelectedCustomer);
    }

    [Fact]
    public void SearchText_NoCustomerMatches_SelectedCustomerBecomesNull()
    {
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService);

        sut.SearchText = "no-such-customer";

        Assert.Empty(sut.Customers);
        Assert.Null(sut.SelectedCustomer);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var customers = new List<CustomerDto> { MakeCustomer("customer-1", "Amelia Hart") };
        var queryService = new StubCustomerQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<CustomerDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<CustomerDto>>(customers));
        var sut = new CustomerPageViewModel(queryService);
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(customers, sut.Customers);
    }
}
