using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using AppBookings = Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Presentation.Tests.Customers;

public sealed class CustomerProfileViewModelTests
{
    private const string PiiSecret = "Amelia Hart / amelia.hart@example.com / 555-0100";

    [Fact]
    public void LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromException<CustomerProfileDto>(new InvalidOperationException(PiiSecret)));
        var logger = new RecordingLogger<CustomerProfileViewModel>();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, new StubCustomerCommandService(), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(PiiSecret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromException<CustomerProfileDto>(new InvalidOperationException("boom")));

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, new StubCustomerCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    private static CustomerProfileDto MakeProfile(string customerId = "customer-1", CustomerBookingSummaryDto? bookingSummary = null, CustomerInsightsDto? insights = null) =>
        new(
            new CustomerDto(customerId, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "555-0100", CustomerStatus.Active, "$4,820", DateTimeOffset.UnixEpoch, "Notes", "org-1", "branch-1"),
            [new CustomerNoteDto("note-1", customerId, "Prefers evenings.", DateTimeOffset.UnixEpoch)],
            [new CustomerTagDto("tag-1", customerId, "Regular", DateTimeOffset.UnixEpoch)],
            [new CustomerActivityDto("activity-1", customerId, "Customer created", DateTimeOffset.UnixEpoch)],
            [new CustomerStatDto("Lifetime Value", "$4,820")],
            bookingSummary ?? CustomerBookingSummaryDto.Empty,
            insights ?? CustomerInsightsDto.Empty);

    private static AppBookings.BookingDto MakeBooking(string id, string customerId, DateTimeOffset scheduledAt, AppBookings.BookingStatus status = AppBookings.BookingStatus.Confirmed) =>
        new(id, customerId, "Amelia Hart", "service-1", "Haircut", "specialist-1", "Alex Stylist",
            scheduledAt, 60, "80", status, string.Empty, "org-1", "branch-1");

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<CustomerProfileDto>();
        var profileQuery = new StubCustomerProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesCustomerNotesTagsActivityAndStatistics()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Amelia Hart", sut.Customer?.FullName);
        Assert.Equal(CustomerStatus.Active, sut.EditableStatus);
        Assert.Single(sut.Notes);
        Assert.Single(sut.Tags);
        Assert.Single(sut.Activity);
        Assert.Single(sut.Statistics);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromException<CustomerProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AddNoteCommand_TextIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.False(sut.AddNoteCommand.CanExecute(null));

        sut.NewNoteText = "A new note";

        Assert.True(sut.AddNoteCommand.CanExecute(null));
    }

    [Fact]
    public void AddNoteCommand_Executed_CallsCommandServiceWithCustomerIdAndTextThenClearsInput()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            NewNoteText = "Prefers evenings.",
        };

        sut.AddNoteCommand.Execute(null);

        var call = Assert.Single(commandService.AddNoteCalls);
        Assert.Equal("customer-1", call.CustomerId);
        Assert.Equal("Prefers evenings.", call.Text);
        Assert.Equal(string.Empty, sut.NewNoteText);
    }

    [Fact]
    public void AddTagCommand_Executed_CallsCommandServiceWithCustomerIdAndLabel()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            NewTagText = "VIP",
        };

        sut.AddTagCommand.Execute(null);

        var call = Assert.Single(commandService.AddTagCalls);
        Assert.Equal("customer-1", call.CustomerId);
        Assert.Equal("VIP", call.Label);
    }

    [Fact]
    public void RemoveTagCommand_Executed_CallsCommandServiceWithCustomerIdAndTagId()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);
        var tag = new CustomerTagDto("tag-1", "customer-1", "Regular", DateTimeOffset.UnixEpoch);

        sut.RemoveTagCommand.Execute(tag);

        var call = Assert.Single(commandService.RemoveTagCalls);
        Assert.Equal("customer-1", call.CustomerId);
        Assert.Equal("tag-1", call.TagId);
    }

    [Fact]
    public void SaveChangesCommand_Executed_CallsUpdateCustomerAsyncWithEditableStatus()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            EditableStatus = CustomerStatus.Churned,
        };

        sut.SaveChangesCommand.Execute(null);

        var request = Assert.Single(commandService.UpdateRequests);
        Assert.Equal("customer-1", request.Id);
        Assert.Equal(CustomerStatus.Churned, request.Status);
    }

    // ---- Phase 8.66: Production Hardening (missing-guard sweep, Wave A) ----

    private const string BackendBody = "HTTP 500: backend response body / customer PII secret";

    [Fact]
    public void AddNoteCommand_BackendThrows_SetsInlineError_DoesNotThrow_PreservesInput_LogsOperationOnly()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService { AddNoteException = new InvalidOperationException(BackendBody) };
        var logger = new RecordingLogger<CustomerProfileViewModel>();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService, logger)
        {
            NewNoteText = "Prefers evenings.",
        };

        var exception = Record.Exception(() => sut.AddNoteCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.SaveErrorMessage);
        Assert.Equal("Prefers evenings.", sut.NewNoteText); // input preserved for retry
        Assert.Equal(DashboardState.Loaded, sut.State); // panel not replaced with the full error view
        var entry = Assert.Single(logger.Entries.FindAll(e => e.Message.Contains("AddNoteAsync", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.DoesNotContain(BackendBody, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTagCommand_BackendThrows_SetsInlineError_DoesNotThrow()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService { AddTagException = new InvalidOperationException(BackendBody) };
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService) { NewTagText = "VIP" };

        var exception = Record.Exception(() => sut.AddTagCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.SaveErrorMessage);
        Assert.Equal("VIP", sut.NewTagText);
    }

    [Fact]
    public void RemoveTagCommand_BackendThrows_SetsInlineError_DoesNotThrow()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService { RemoveTagException = new InvalidOperationException(BackendBody) };
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);
        var tag = new CustomerTagDto("tag-1", "customer-1", "Regular", DateTimeOffset.UnixEpoch);

        var exception = Record.Exception(() => sut.RemoveTagCommand.Execute(tag));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.SaveErrorMessage);
    }

    [Fact]
    public void SaveChangesCommand_BackendThrows_SetsInlineError_RevertsEditableStatus_DoesNotThrow()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile())); // MakeProfile's customer is Active
        var commandService = new StubCustomerCommandService { UpdateCustomerException = new InvalidOperationException(BackendBody) };
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            EditableStatus = CustomerStatus.Churned,
        };

        var exception = Record.Exception(() => sut.SaveChangesCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.SaveErrorMessage);
        Assert.Equal(CustomerStatus.Active, sut.EditableStatus); // reverted - a rejected change is never left displayed as applied
        Assert.Equal(CustomerStatus.Active, sut.Customer?.Status);
    }

    [Fact]
    public void AddNoteCommand_Succeeds_ClearsAnyPriorInlineError()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService { AddNoteException = new InvalidOperationException("boom") };
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService) { NewNoteText = "first" };
        sut.AddNoteCommand.Execute(null);
        Assert.True(sut.HasSaveError);

        commandService.AddNoteException = null;
        sut.NewNoteText = "second";
        sut.AddNoteCommand.Execute(null);

        Assert.False(sut.HasSaveError);
        Assert.Null(sut.SaveErrorMessage);
    }

    // Sprint 4 Commit 3: customer 360 booking integration.

    [Fact]
    public void Constructor_ProfileHasBookingSummary_PopulatesAppointmentsAndBookingStatistics()
    {
        var now = DateTimeOffset.Now;
        var upcoming = MakeBooking("booking-upcoming", "customer-1", now.AddDays(5));
        var past = MakeBooking("booking-past", "customer-1", now.AddDays(-5), AppBookings.BookingStatus.Completed);
        var bookingSummary = new CustomerBookingSummaryDto([upcoming], [past], TotalBookingCount: 2, CompletedBookingCount: 1, LastAppointmentDate: past.ScheduledAt);
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile(bookingSummary: bookingSummary)));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal([upcoming], sut.UpcomingAppointments);
        Assert.Equal([past], sut.PastAppointments);
        Assert.Equal(2, sut.TotalBookingCount);
        Assert.Equal(1, sut.CompletedBookingCount);
        Assert.Equal(past.ScheduledAt, sut.LastAppointmentDate);
    }

    [Fact]
    public void Constructor_ProfileHasNoBookings_UpcomingAndPastAppointmentsAreEmpty()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Empty(sut.UpcomingAppointments);
        Assert.Empty(sut.PastAppointments);
        Assert.Equal(0, sut.TotalBookingCount);
        Assert.Equal(0, sut.CompletedBookingCount);
        Assert.Null(sut.LastAppointmentDate);
    }

    // Sprint 4 Commit 4: premium customer 360 UI - ActivityTimeline re-shapes Activity for the
    // Shared Timeline control (see CustomerProfileViewModel's own doc comment for why this is a
    // live ViewModel-owned collection rather than an XAML value converter).

    [Fact]
    public void Constructor_ProfileHasActivity_PopulatesActivityTimelineWithMatchingEntries()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        var entry = Assert.Single(sut.ActivityTimeline);
        Assert.Equal("Customer created", entry.Title);
        Assert.False(string.IsNullOrEmpty(entry.Icon));
    }

    [Fact]
    public void AddNoteCommand_Executed_ReloadKeepsActivityTimelineInSyncWithActivity()
    {
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            NewNoteText = "Prefers evenings.",
        };

        sut.AddNoteCommand.Execute(null);

        Assert.Equal(sut.Activity.Count, sut.ActivityTimeline.Count);
        Assert.Equal(sut.Activity.Select(activity => activity.Description), sut.ActivityTimeline.Select(entry => entry.Title));
    }

    // Sprint 4 Commit 5: customer intelligence insights. CustomerInsightsCalculator itself is
    // tested exhaustively in Application.Tests - these only verify the ViewModel exposes whatever
    // CustomerProfileDto.Insights it was given, unchanged.

    [Fact]
    public void Constructor_ProfileHasInsights_ExposesCustomerScoreLoyaltyLevelAndEngagementStatus()
    {
        var insights = new CustomerInsightsDto(
            CustomerScore: 72,
            LoyaltyLevel: LoyaltyLevel.Loyal,
            EngagementStatus: EngagementStatus.Active,
            VisitCount: 5,
            CompletedVisitCount: 4,
            LastVisitDate: DateTimeOffset.UnixEpoch,
            UpcomingVisitCount: 1,
            DaysSinceLastVisit: 12);
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile(insights: insights)));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(72, sut.CustomerScore);
        Assert.Equal(LoyaltyLevel.Loyal, sut.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Active, sut.EngagementStatus);
        Assert.Equal(12, sut.DaysSinceLastVisit);
    }

    [Fact]
    public void Constructor_ProfileHasNoBookingsOrActivityInsights_EmptyInsightStateIsExposedCorrectly()
    {
        // CustomerInsightsDto.Empty - the "customer with no bookings" default the Application-layer
        // calculator itself produces (see CustomerInsightsCalculatorTests) - must render as a
        // well-defined empty state here too, never a crash or an unset value.
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(0, sut.CustomerScore);
        Assert.Equal(LoyaltyLevel.New, sut.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Inactive, sut.EngagementStatus);
        Assert.Null(sut.DaysSinceLastVisit);
    }

    [Fact]
    public void AddNoteCommand_Executed_ReloadKeepsInsightsInSyncWithLatestProfile()
    {
        var initialInsights = new CustomerInsightsDto(10, LoyaltyLevel.Regular, EngagementStatus.AtRisk, 1, 1, DateTimeOffset.UnixEpoch, 0, 90);
        var updatedInsights = new CustomerInsightsDto(30, LoyaltyLevel.Loyal, EngagementStatus.Active, 1, 1, DateTimeOffset.UnixEpoch, 0, 5);
        var shouldReturnUpdated = false;
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(
            MakeProfile(insights: shouldReturnUpdated ? updatedInsights : initialInsights)));
        var commandService = new StubCustomerCommandService();
        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService)
        {
            NewNoteText = "Prefers evenings.",
        };
        Assert.Equal(LoyaltyLevel.Regular, sut.LoyaltyLevel);

        shouldReturnUpdated = true;
        sut.AddNoteCommand.Execute(null);

        Assert.Equal(30, sut.CustomerScore);
        Assert.Equal(LoyaltyLevel.Loyal, sut.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Active, sut.EngagementStatus);
        Assert.Equal(5, sut.DaysSinceLastVisit);
    }

    // Sprint 4 Commit 6: end-to-end regression - the full Customer 360 surface (identity, notes,
    // tags, activity/timeline, statistics, bookings, and Customer Intelligence) populated together
    // from one profile load, the shape CustomerPage.xaml's Premium UI actually binds against.

    [Fact]
    public void Constructor_FullProfile_ExposesCompleteCustomer360State()
    {
        var now = DateTimeOffset.Now;
        var upcoming = MakeBooking("booking-upcoming", "customer-1", now.AddDays(5));
        var past = MakeBooking("booking-past", "customer-1", now.AddDays(-10), AppBookings.BookingStatus.Completed);
        var bookingSummary = new CustomerBookingSummaryDto([upcoming], [past], TotalBookingCount: 2, CompletedBookingCount: 1, LastAppointmentDate: past.ScheduledAt);
        var insights = new CustomerInsightsDto(
            CustomerScore: 40,
            LoyaltyLevel: LoyaltyLevel.Regular,
            EngagementStatus: EngagementStatus.Active,
            VisitCount: 2,
            CompletedVisitCount: 1,
            LastVisitDate: past.ScheduledAt,
            UpcomingVisitCount: 1,
            DaysSinceLastVisit: 10);
        var profileQuery = new StubCustomerProfileQueryService((_, _) => Task.FromResult(
            MakeProfile(bookingSummary: bookingSummary, insights: insights)));
        var commandService = new StubCustomerCommandService();

        var sut = new CustomerProfileViewModel("customer-1", profileQuery, commandService);

        // Identity, notes, tags, activity/timeline, statistics - every field CustomerPage.xaml's
        // identity header, Tags card, Notes card, and Timeline widget bind to.
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Amelia Hart", sut.Customer?.FullName);
        Assert.Equal(CustomerStatus.Active, sut.EditableStatus);
        Assert.Single(sut.Notes);
        Assert.Single(sut.Tags);
        Assert.Single(sut.Activity);
        Assert.Single(sut.ActivityTimeline);
        Assert.Single(sut.Statistics);

        // Booking integration (Sprint 4 Commit 3) - the Appointments sections' bindings.
        Assert.Equal([upcoming], sut.UpcomingAppointments);
        Assert.Equal([past], sut.PastAppointments);
        Assert.Equal(2, sut.TotalBookingCount);
        Assert.Equal(1, sut.CompletedBookingCount);
        Assert.Equal(past.ScheduledAt, sut.LastAppointmentDate);

        // Customer Intelligence (Sprint 4 Commit 5) - the Insights section's bindings.
        Assert.Equal(40, sut.CustomerScore);
        Assert.Equal(LoyaltyLevel.Regular, sut.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Active, sut.EngagementStatus);
        Assert.Equal(10, sut.DaysSinceLastVisit);
    }
}
