using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using AppBookings = Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Presentation.Tests.Customers;

public sealed class CustomerProfileViewModelTests
{
    private static CustomerProfileDto MakeProfile(string customerId = "customer-1", CustomerBookingSummaryDto? bookingSummary = null) =>
        new(
            new CustomerDto(customerId, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "555-0100", CustomerStatus.Active, "$4,820", DateTimeOffset.UnixEpoch, "Notes", "org-1", "branch-1"),
            [new CustomerNoteDto("note-1", customerId, "Prefers evenings.", DateTimeOffset.UnixEpoch)],
            [new CustomerTagDto("tag-1", customerId, "Regular", DateTimeOffset.UnixEpoch)],
            [new CustomerActivityDto("activity-1", customerId, "Customer created", DateTimeOffset.UnixEpoch)],
            [new CustomerStatDto("Lifetime Value", "$4,820")],
            bookingSummary ?? CustomerBookingSummaryDto.Empty);

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
}
