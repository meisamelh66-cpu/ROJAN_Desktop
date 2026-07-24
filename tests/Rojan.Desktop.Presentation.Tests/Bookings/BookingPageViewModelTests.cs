using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Presentation.Tests.BookingWorkflow;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Bookings;

public sealed class BookingPageViewModelTests
{
    private static BookingDto MakeBooking(string id, string customerName, BookingStatus status = BookingStatus.Pending) =>
        new(id, string.Empty, customerName, string.Empty, "Test Service", string.Empty, "Test Specialist",
            DateTimeOffset.UnixEpoch, 60, "$0", status, string.Empty, "org-1", "branch-1");

    private static BookingPageViewModel MakeSut(
        StubBookingQueryService queryService,
        StubBookingCommandService? commandService = null,
        StubBookingWorkflowService? workflowService = null,
        StubDialogService? dialogService = null) =>
        new(queryService, commandService ?? new StubBookingCommandService(), workflowService ?? new StubBookingWorkflowService(), dialogService ?? new StubDialogService());

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<BookingDto>>();
        var queryService = new StubBookingQueryService(_ => tcs.Task);

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsBookings_StateIsLoadedAndSelectsFirstBooking()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(bookings, sut.Bookings);
        Assert.Equal(bookings[0], sut.SelectedBooking);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedBooking);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubBookingQueryService(
            _ => Task.FromException<IReadOnlyList<BookingDto>>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService);

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
        var sut = MakeSut(queryService);
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
        var sut = MakeSut(queryService);

        Assert.False(sut.CreateBookingCommand.CanExecute(null));

        sut.NewBookingCustomerName = "Grace Kim";
        sut.NewBookingServiceName = "Facial";

        Assert.True(sut.CreateBookingCommand.CanExecute(null));
    }

    [Fact]
    public void CreateBookingCommand_DateIsNull_CanExecuteIsFalse()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);
        sut.NewBookingCustomerName = "Grace Kim";
        sut.NewBookingServiceName = "Facial";
        sut.NewBookingDate = null;

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
        var sut = MakeSut(queryService, commandService);
        sut.NewBookingCustomerName = "Grace Kim";
        sut.NewBookingServiceName = "Facial";
        sut.NewBookingSpecialistName = "Casey Morgan";

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
        var sut = MakeSut(queryService);

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
        var sut = MakeSut(queryService);

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
        var sut = MakeSut(queryService);

        Assert.Equal(expectedCanExecute, sut.CancelBookingCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmBookingCommand_Executed_CallsCommandServiceWithSelectedBookingIdAndConfirmedStatus()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Pending) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = MakeSut(queryService, commandService);

        sut.ConfirmBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.Confirmed, call.Status);
    }

    [Fact]
    public void CancelBookingCommand_Executed_CallsWorkflowServiceCancelBookingAsyncWithSelectedBookingId()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, commandService, workflowService);

        sut.CancelBookingCommand.Execute(null);

        var cancelledId = Assert.Single(workflowService.CancelledBookingIds);
        Assert.Equal("booking-1", cancelledId);
    }

    [Fact]
    public void CancelBookingCommand_Executed_DoesNotCallCommandServiceUpdateBookingStatusAsyncDirectly()
    {
        // The workflow service internally calls IBookingCommandService.UpdateBookingStatusAsync as
        // part of cancel-and-release-calendar-slot, but that must happen inside
        // IBookingWorkflowService.CancelBookingAsync - never as a separate direct call from
        // BookingPageViewModel, or the Calendar release would be skipped (Sprint 3 Commit 1 fix).
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Pending) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, commandService, workflowService);

        sut.CancelBookingCommand.Execute(null);

        Assert.Empty(commandService.UpdateStatusCalls);
    }

    [Fact]
    public void ConfirmBookingCommand_Executed_StillCallsCommandServiceDirectly_NotWorkflowService()
    {
        // Confirm/Complete are unaffected by the Cancel fix - they never reserved or release a
        // Calendar slot, so they should keep going through IBookingCommandService directly.
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Pending) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, commandService, workflowService);

        sut.ConfirmBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.Confirmed, call.Status);
        Assert.Empty(workflowService.CancelledBookingIds);
    }

    [Fact]
    public void OpenWizardCommand_Executed_ShowsBookingWizardViewModelViaDialogService()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var dialogService = new StubDialogService();
        var sut = MakeSut(queryService, dialogService: dialogService);

        sut.OpenWizardCommand.Execute(null);

        var shown = Assert.Single(dialogService.ShownDialogs);
        Assert.IsType<Rojan.Desktop.Presentation.ViewModels.BookingWorkflow.BookingWizardViewModel>(shown);
    }
}
