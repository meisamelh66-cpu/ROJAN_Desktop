using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Tests.BookingWorkflow;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;
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
        StubDialogService? dialogService = null,
        ILogger<BookingPageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null) =>
        new(queryService, commandService ?? new StubBookingCommandService(), workflowService ?? new StubBookingWorkflowService(), dialogService ?? new StubDialogService(), logger, loggerFactory);

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
    [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.InProgress, true)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.NoShow, false)]
    public void CompleteBookingCommand_CanExecute_TrueOnlyWhenSelectedBookingIsInProgress(BookingStatus status, bool expectedCanExecute)
    {
        // Completed is only reachable via InProgress per BookingRules - Confirmed must NOT allow
        // Complete (Sprint 3 Commit 3 fix; the old CanExecute incorrectly allowed Confirmed, which
        // let the button call an illegal Confirmed -> Completed transition and throw at runtime).
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);

        Assert.Equal(expectedCanExecute, sut.CompleteBookingCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.InProgress, false)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.NoShow, false)]
    public void StartBookingCommand_CanExecute_TrueWhenPendingOrConfirmed(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);

        Assert.Equal(expectedCanExecute, sut.StartBookingCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, false)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.InProgress, false)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.NoShow, false)]
    public void NoShowBookingCommand_CanExecute_TrueOnlyWhenSelectedBookingIsConfirmed(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);

        Assert.Equal(expectedCanExecute, sut.NoShowBookingCommand.CanExecute(null));
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
    public void StartBookingCommand_Executed_CallsCommandServiceWithSelectedBookingIdAndInProgressStatus()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = MakeSut(queryService, commandService);

        sut.StartBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.InProgress, call.Status);
    }

    [Fact]
    public void CompleteBookingCommand_Executed_CallsCommandServiceWithSelectedBookingIdAndCompletedStatus()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.InProgress) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = MakeSut(queryService, commandService);

        sut.CompleteBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.Completed, call.Status);
    }

    [Fact]
    public void NoShowBookingCommand_Executed_CallsCommandServiceWithSelectedBookingIdAndNoShowStatus()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = MakeSut(queryService, commandService);

        sut.NoShowBookingCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateStatusCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(BookingStatus.NoShow, call.Status);
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

    [Fact]
    public void OpenWizardCommand_ForwardsLoggerFactoryToWizard_ChildLoadFailureIsLoggedViaTheFactory()
    {
        const string secret = "guest booking secret / 555-0100";
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromException<BookingOptionsDto>(new InvalidOperationException(secret)));
        var dialogService = new StubDialogService();
        var loggerFactory = new RecordingLoggerFactory();
        var sut = MakeSut(queryService, workflowService: workflowService, dialogService: dialogService, loggerFactory: loggerFactory);

        sut.OpenWizardCommand.Execute(null);

        Assert.Single(dialogService.ShownDialogs);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(BookingWizardViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=LoadOptionsAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_NoFilterApplied_SearchesWithAnAllDefaultFilter()
    {
        // "Keep existing booking list behavior unchanged when no filter is applied" -
        // an all-default BookingSearchFilter is documented to behave identically to the old
        // unfiltered GetBookingsAsync call (see BookingSearchFilter's own doc comment).
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));

        var sut = MakeSut(queryService);

        var filter = Assert.Single(queryService.SearchCalls);
        Assert.Null(filter.SearchText);
        Assert.Null(filter.CustomerName);
        Assert.Null(filter.ServiceName);
        Assert.Null(filter.Status);
        Assert.Null(filter.DateFrom);
        Assert.Null(filter.DateTo);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(bookings, sut.Bookings);
    }

    [Fact]
    public void SearchText_Changed_SearchesWithSearchTextInFilter()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        sut.SearchText = "amelia";

        Assert.Equal("amelia", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void CustomerNameFilter_Changed_SearchesWithCustomerNameInFilter()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        sut.CustomerNameFilter = "Hart";

        Assert.Equal("Hart", queryService.SearchCalls[^1].CustomerName);
    }

    [Fact]
    public void ServiceNameFilter_Changed_SearchesWithServiceNameInFilter()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        sut.ServiceNameFilter = "Haircut";

        Assert.Equal("Haircut", queryService.SearchCalls[^1].ServiceName);
    }

    [Fact]
    public void StatusFilter_Changed_SearchesWithStatusInFilter()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        sut.StatusFilter = BookingStatus.Confirmed;

        Assert.Equal(BookingStatus.Confirmed, queryService.SearchCalls[^1].Status);
    }

    [Fact]
    public void DateRangeFilters_Changed_SearchesWithDateRangeInFilter()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        sut.DateFromFilter = new DateTime(2026, 3, 1);
        sut.DateToFilter = new DateTime(2026, 3, 31);

        var filter = queryService.SearchCalls[^1];
        Assert.Equal(new DateOnly(2026, 3, 1), filter.DateFrom);
        Assert.Equal(new DateOnly(2026, 3, 31), filter.DateTo);
    }

    [Fact]
    public void StatusFilterOptions_FirstEntryIsNull_FollowedByEveryBookingStatus()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var sut = MakeSut(queryService);

        Assert.Null(sut.StatusFilterOptions[0]);
        Assert.Equal(Enum.GetValues<BookingStatus>().Length + 1, sut.StatusFilterOptions.Count);
    }

    [Fact]
    public void ConfirmBookingCommand_Executed_ReloadPreservesActiveFilter()
    {
        // A status-transition action's reload must not silently drop the user's active filter -
        // this is the "filter survives a Confirm/Complete/Cancel/Create action" behavior Sprint 3
        // Commit 2 adds by routing every load through SearchBookingsAsync.
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Pending) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService();
        var sut = MakeSut(queryService, commandService);
        sut.CustomerNameFilter = "Amelia";

        sut.ConfirmBookingCommand.Execute(null);

        Assert.Equal("Amelia", queryService.SearchCalls[^1].CustomerName);
    }

    // Sprint 3 Commit 6: reschedule workflow wiring.

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.InProgress, true)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.NoShow, false)]
    public void RescheduleBookingCommand_CanExecute_TrueOnlyWhenActiveAndDateChosen(BookingStatus status, bool expectedCanExecute)
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", status) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);
        sut.RescheduleDate = new DateTime(2026, 3, 15);

        Assert.Equal(expectedCanExecute, sut.RescheduleBookingCommand.CanExecute(null));
    }

    [Fact]
    public void RescheduleBookingCommand_CanExecute_FalseWhenNoDateChosen()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);

        Assert.Null(sut.RescheduleDate);
        Assert.False(sut.RescheduleBookingCommand.CanExecute(null));
    }

    [Fact]
    public void RescheduleBookingCommand_Executed_CallsWorkflowServiceWithSelectedBookingIdAndNewDateTime_PreservingTimeOfDay()
    {
        var originalScheduledAt = new DateTimeOffset(2026, 3, 1, 14, 30, 0, TimeSpan.Zero);
        var bookings = new List<BookingDto>
        {
            new("booking-1", string.Empty, "Amelia Hart", string.Empty, "Test Service", "specialist-1", "Jordan Lee",
                originalScheduledAt, 60, "$0", BookingStatus.Confirmed, string.Empty, "org-1", "branch-1"),
        };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, workflowService: workflowService);
        sut.RescheduleDate = new DateTime(2026, 3, 20);

        sut.RescheduleBookingCommand.Execute(null);

        var call = Assert.Single(workflowService.RescheduleCalls);
        Assert.Equal("booking-1", call.BookingId);
        Assert.Equal(new DateTimeOffset(2026, 3, 20, 14, 30, 0, TimeSpan.Zero), call.NewSlotStart);
    }

    [Fact]
    public void RescheduleBookingCommand_Executed_NeverCallsCommandServiceDirectly()
    {
        // Reschedule must go through IBookingWorkflowService only - never IBookingCommandService
        // directly, or the Calendar release/reserve orchestration would be skipped entirely (same
        // reasoning as the Sprint 3 Commit 1 Cancel fix). StubBookingCommandService.RescheduleBookingAsync
        // throws NotSupportedException, so this test would fail loudly if the ViewModel ever called it.
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var sut = MakeSut(queryService);
        sut.RescheduleDate = new DateTime(2026, 3, 20);

        sut.RescheduleBookingCommand.Execute(null);
    }

    [Fact]
    public void RescheduleBookingCommand_Executed_ClearsRescheduleDateAndReloads()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, workflowService: workflowService);
        sut.RescheduleDate = new DateTime(2026, 3, 20);

        sut.RescheduleBookingCommand.Execute(null);

        Assert.Null(sut.RescheduleDate);
        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void RescheduleBookingCommand_Executed_ReloadPreservesActiveFilter()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var workflowService = new StubBookingWorkflowService();
        var sut = MakeSut(queryService, workflowService: workflowService);
        sut.CustomerNameFilter = "Amelia";
        sut.RescheduleDate = new DateTime(2026, 3, 20);

        sut.RescheduleBookingCommand.Execute(null);

        Assert.Equal("Amelia", queryService.SearchCalls[^1].CustomerName);
    }

    // Phase 7.4.4 Booking/Checkout Error Hardening: CreateBookingAsync/ChangeStatusAsync/
    // CancelSelectedBookingAsync/RescheduleSelectedBookingAsync previously had no try/catch at all
    // - these tests exercise the new guards directly, not just that the app "doesn't crash".

    [Fact]
    public void CreateBookingCommand_BackendThrows_SetsErrorStateAndLogsWithoutClearingForm()
    {
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var commandService = new StubBookingCommandService { CreateFailure = new InvalidOperationException("boom") };
        var logger = new RecordingLogger<BookingPageViewModel>();
        var sut = MakeSut(queryService, commandService, logger: logger);
        sut.NewBookingCustomerName = "Amelia Hart";
        sut.NewBookingServiceName = "Haircut";
        sut.NewBookingDate = new DateTime(2026, 3, 20);

        sut.CreateBookingCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        // The user's input must survive a failed submission so they can retry, not lose it.
        Assert.Equal("Amelia Hart", sut.NewBookingCustomerName);
        Assert.Equal("Haircut", sut.NewBookingServiceName);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void ConfirmBookingCommand_BackendThrows_SetsErrorState()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var commandService = new StubBookingCommandService { UpdateStatusFailure = new InvalidOperationException("boom") };
        var logger = new RecordingLogger<BookingPageViewModel>();
        var sut = MakeSut(queryService, commandService, logger: logger);

        sut.ConfirmBookingCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void CancelBookingCommand_WorkflowThrows_SetsErrorState()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart") };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var workflowService = new StubBookingWorkflowService { CancelFailure = new InvalidOperationException("boom") };
        var logger = new RecordingLogger<BookingPageViewModel>();
        var sut = MakeSut(queryService, workflowService: workflowService, logger: logger);

        sut.CancelBookingCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void RescheduleBookingCommand_WorkflowThrows_SetsErrorStateAndDoesNotClearRescheduleDate()
    {
        var bookings = new List<BookingDto> { MakeBooking("booking-1", "Amelia Hart", BookingStatus.Confirmed) };
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>(bookings));
        var workflowService = new StubBookingWorkflowService(
            rescheduleBooking: (_, _, _) => Task.FromException<Application.BookingWorkflow.BookingConfirmationDto>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<BookingPageViewModel>();
        var sut = MakeSut(queryService, workflowService: workflowService, logger: logger);
        sut.RescheduleDate = new DateTime(2026, 3, 20);

        sut.RescheduleBookingCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.NotNull(sut.RescheduleDate);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_CreateBookingFailureNeverThrows()
    {
        // The optional-logger default (NullLogger) must be a genuinely safe no-op - a failure here
        // would mean every existing test/call site that doesn't pass a logger (all of them, before
        // this Phase) was silently relying on undefined behavior.
        var queryService = new StubBookingQueryService(_ => Task.FromResult<IReadOnlyList<BookingDto>>([]));
        var commandService = new StubBookingCommandService { CreateFailure = new InvalidOperationException("boom") };
        var sut = MakeSut(queryService, commandService);
        sut.NewBookingCustomerName = "Amelia Hart";
        sut.NewBookingServiceName = "Haircut";
        sut.NewBookingDate = new DateTime(2026, 3, 20);

        var exception = Record.Exception(() => sut.CreateBookingCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(DashboardState.Error, sut.State);
    }
}
