using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Bookings;

public sealed class BookingPageViewModelTests
{
    private static BookingDto MakeBooking(string id, string customerName, BookingStatus status = BookingStatus.Pending) =>
        new(id, string.Empty, customerName, "Test Service", "Test Specialist", DateTimeOffset.UnixEpoch, 60, status, string.Empty);

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<BookingDto>>();
        var queryService = new StubBookingQueryService(_ => tcs.Task);

        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsBookings_StateIsLoadedAndSelectsFirstBooking()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));

        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(bookings, sut.Bookings);
        Assert.Equal(bookings[0], sut.SelectedBooking);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));

        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedBooking);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubBookingQueryService(
            _ => Task.FromException<IReadOnlyList<BookingDto>>(new InvalidOperationException("boom")));

        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<BookingDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(bookings, sut.Bookings);
    }

    [Fact]
    public void CreateBookingCommand_RequiredFieldsMissing_CanExecuteIsFalse()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.False(sut.CreateBookingCommand.CanExecute(null));

        sut.NewBookingCustomerName = "Grace Kim";
        sut.NewBookingServiceName = "Facial";

        Assert.True(sut.CreateBookingCommand.CanExecute(null));
    }

    [Fact]
    public void CreateBookingCommand_DateIsNull_CanExecuteIsFalse()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService())
        {
            NewBookingCustomerName = "Grace Kim",
            NewBookingServiceName = "Facial",
            NewBookingDate = null,
        };

        Assert.False(sut.CreateBookingCommand.CanExecute(null));
    }

    [Fact]
    public void CreateBookingCommand_Executed_CallsCommandServiceReloadsListAndSelectsNewBooking()
    {
        var existing = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(existing.ToList()));
        var commandService = new StubBookingCommandService
        {
            OnBookingCreated = (_, dto) => existing.Add(dto),
        };
        var sut = new BookingPageViewModel(queryService, commandService)
        {
            NewBookingCustomerName = "Grace Kim",
            NewBookingServiceName = "Facial",
            NewBookingSpecialistName = "Casey Morgan",
        };

        sut.CreateBookingCommand.Execute(null);

        var request = Assert.Single(commandService.CreateRequests);
        Assert.Equal("Grace Kim", request.CustomerName);
        Assert.Equal(string.Empty, sut.NewBookingCustomerName);
        Assert.Equal("new-booking", sut.SelectedBooking?.Id);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void ConfirmBookingCommand_CanExecute_TrueOnlyWhenSelectedBookingIsPending(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(expectedCanExecute, sut.ConfirmBookingCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, false)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CompleteBookingCommand_CanExecute_TrueOnlyWhenSelectedBookingIsConfirmed(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(expectedCanExecute, sut.CompleteBookingCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CancelBookingCommand_CanExecute_TrueWhenPendingOrConfirmed(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = new BookingPageViewModel(queryService, new StubBookingCommandService());

        Assert.Equal(expectedCanExecute, sut.CancelBookingCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmBookingCommand_Executed_CallsCommandServiceWithSelectedBookingIdAndConfirmedStatus()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Pending) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = new BookingPageViewModel(queryService, commandService);

        sut.ConfirmBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.Confirmed, call.Status);
    }
}
