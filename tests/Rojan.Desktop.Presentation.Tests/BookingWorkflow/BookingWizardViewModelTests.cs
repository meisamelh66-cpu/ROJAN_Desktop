using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.BookingWorkflow;

public sealed class BookingWizardViewModelTests
{
    private static readonly DateTimeOffset SlotStart = new(2026, 3, 2, 9, 0, 0, DateTimeOffset.Now.Offset);

    private static WorkflowCustomerOptionDto MakeCustomer() => new("customer-1", "Amelia Hart", IsLinkedToAccount: true);

    private static WorkflowServiceOptionDto MakeService() => new("service-1", "Haircut & Style", 60, "$65");

    /// <summary>Empty AssignedServiceIds - ROJAN_Backend's own "unrestricted, eligible for everything" default (Booking Eligibility Filter), preserving every existing test in this file unchanged.</summary>
    private static WorkflowSpecialistOptionDto MakeSpecialist(IReadOnlyList<string>? assignedServiceIds = null) =>
        new("specialist-1", "Jordan Lee", assignedServiceIds ?? []);

    private static BookingOptionsDto MakeOptions() =>
        new([MakeCustomer()], [MakeService()], [MakeSpecialist()]);

    [Fact]
    public void Constructor_OptionsLoad_StateIsLoadedAndCollectionsPopulated()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));

        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Customers);
        Assert.Single(sut.Services);
        Assert.Single(sut.Specialists);
        Assert.Equal(BookingWizardStep.Customer, sut.CurrentStep);
    }

    [Fact]
    public void Constructor_OptionsQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromException<BookingOptionsDto>(new InvalidOperationException("boom")));

        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void NextCommand_NoCustomerSelected_CanExecuteIsFalse()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        Assert.False(sut.NextCommand.CanExecute(null));

        sut.SelectedCustomer = MakeCustomer();

        Assert.True(sut.NextCommand.CanExecute(null));
    }

    [Fact]
    public void BackCommand_OnFirstStep_CanExecuteIsFalse()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        Assert.False(sut.BackCommand.CanExecute(null));
    }

    private static BookingWizardViewModel MakeSutOnDateStep(StubBookingWorkflowService workflowService)
    {
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService())
        {
            SelectedCustomer = MakeCustomer(),
        };
        sut.NextCommand.Execute(null); // Customer -> Service
        sut.SelectedService = MakeService();
        sut.NextCommand.Execute(null); // Service -> Specialist
        sut.SelectedSpecialist = MakeSpecialist();
        sut.NextCommand.Execute(null); // Specialist -> Date
        return sut;
    }

    [Fact]
    public void NextCommand_FromDateStep_LoadsAvailableSlotsAndAdvancesToTimeSlot()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            getSlots: (_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(30))]));
        var sut = MakeSutOnDateStep(workflowService);

        sut.NextCommand.Execute(null); // Date -> TimeSlot

        Assert.Equal(BookingWizardStep.TimeSlot, sut.CurrentStep);
        Assert.Single(sut.AvailableSlots);
        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void NextCommand_FromDateStep_NoAvailableSlots_StateIsEmpty()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            getSlots: (_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([]));
        var sut = MakeSutOnDateStep(workflowService);

        sut.NextCommand.Execute(null); // Date -> TimeSlot

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void ConfirmBookingCommand_CanExecute_TrueOnlyOnReviewStep()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            getSlots: (_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60))]));
        var sut = MakeSutOnDateStep(workflowService);

        Assert.False(sut.ConfirmBookingCommand.CanExecute(null));

        sut.NextCommand.Execute(null); // Date -> TimeSlot
        sut.SelectedSlot = new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60));
        sut.NextCommand.Execute(null); // TimeSlot -> Review

        Assert.True(sut.ConfirmBookingCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmBookingCommand_Executed_CallsWorkflowServiceAndAdvancesToConfirmation()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            getSlots: (_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60))]));
        var created = false;
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService(), () => created = true)
        {
            SelectedCustomer = MakeCustomer(),
        };
        sut.NextCommand.Execute(null); // Customer -> Service
        sut.SelectedService = MakeService();
        sut.NextCommand.Execute(null); // Service -> Specialist
        sut.SelectedSpecialist = MakeSpecialist();
        sut.NextCommand.Execute(null); // Specialist -> Date
        sut.NextCommand.Execute(null); // Date -> TimeSlot
        sut.SelectedSlot = new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60));
        sut.NextCommand.Execute(null); // TimeSlot -> Review

        sut.ConfirmBookingCommand.Execute(null);

        Assert.Single(workflowService.CreateRequests);
        Assert.Equal(BookingWizardStep.Confirmation, sut.CurrentStep);
        Assert.NotNull(sut.Confirmation);
        Assert.True(created);
    }

    [Fact]
    public void CancelCommand_Executed_ClosesDialog()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));
        var dialogService = new StubDialogService();
        var sut = new BookingWizardViewModel(workflowService, dialogService);

        sut.CancelCommand.Execute(null);

        Assert.Equal(1, dialogService.CloseCount);
    }

    [Fact]
    public void DoneCommand_Executed_ClosesDialog()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));
        var dialogService = new StubDialogService();
        var sut = new BookingWizardViewModel(workflowService, dialogService);

        sut.DoneCommand.Execute(null);

        Assert.Equal(1, dialogService.CloseCount);
    }

    // ---- Reception Stabilization Sprint: walk-in / guest customer ----

    [Fact]
    public void ConfirmBookingCommand_Executed_CustomerNotLinkedToAccount_SetsErrorMessageAndDoesNotCreateBooking()
    {
        var unlinkedCustomer = new WorkflowCustomerOptionDto("customer-9", "Walk-in Guest", IsLinkedToAccount: false);
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            getSlots: (_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60))]));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService())
        {
            SelectedCustomer = unlinkedCustomer,
        };
        sut.NextCommand.Execute(null); // Customer -> Service
        sut.SelectedService = MakeService();
        sut.NextCommand.Execute(null); // Service -> Specialist
        sut.SelectedSpecialist = MakeSpecialist();
        sut.NextCommand.Execute(null); // Specialist -> Date
        sut.NextCommand.Execute(null); // Date -> TimeSlot
        sut.SelectedSlot = new WorkflowSlotDto(SlotStart, SlotStart.AddMinutes(60));
        sut.NextCommand.Execute(null); // TimeSlot -> Review

        sut.ConfirmBookingCommand.Execute(null);

        Assert.Empty(workflowService.CreateRequests);
        Assert.Equal(BookingWizardStep.Review, sut.CurrentStep);
        Assert.False(string.IsNullOrEmpty(sut.ErrorMessage));
    }

    [Fact]
    public void AddGuestCustomerCommand_CanExecute_FalseWhenGuestFullNameIsEmpty()
    {
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(MakeOptions()));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        Assert.False(sut.AddGuestCustomerCommand.CanExecute(null));

        sut.GuestFullName = "Walk-in Guest";

        Assert.True(sut.AddGuestCustomerCommand.CanExecute(null));
    }

    [Fact]
    public void AddGuestCustomerCommand_Executed_AddsAndSelectsNewUnlinkedCustomer()
    {
        var workflowService = new StubBookingWorkflowService(
            getOptions: _ => Task.FromResult(MakeOptions()),
            createGuestCustomer: (fullName, _, _) => Task.FromResult(new WorkflowCustomerOptionDto("guest-1", fullName, IsLinkedToAccount: false)));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService())
        {
            GuestFullName = "Walk-in Guest",
            GuestPhone = "555-0100",
        };

        sut.AddGuestCustomerCommand.Execute(null);

        var createCall = Assert.Single(workflowService.CreateGuestCustomerCalls);
        Assert.Equal("Walk-in Guest", createCall.FullName);
        Assert.Equal("555-0100", createCall.Phone);
        Assert.Contains(sut.Customers, c => c.Id == "guest-1");
        Assert.Equal("guest-1", sut.SelectedCustomer?.Id);
        Assert.False(sut.SelectedCustomer?.IsLinkedToAccount);
        Assert.Equal(string.Empty, sut.GuestFullName);
    }

    // ---- Booking Eligibility Filter ----

    [Fact]
    public void SelectedService_SpecialistHasNoAssignments_IsEligibleForEveryService()
    {
        // The single most important case: ROJAN_Backend's own "unrestricted, eligible for
        // everything" default - a naive "is this service in the list" filter would get this
        // backwards, hiding every specialist who hasn't been assigned anything yet.
        var options = new BookingOptionsDto([MakeCustomer()], [MakeService()], [MakeSpecialist()]);
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        sut.SelectedService = MakeService();

        Assert.Single(sut.EligibleSpecialists);
        Assert.False(sut.HasNoEligibleSpecialists);
    }

    [Fact]
    public void SelectedService_SpecialistHasAssignments_OnlyEligibleForAssignedService()
    {
        var eligibleSpecialist = MakeSpecialist(["service-1"]);
        var ineligibleSpecialist = new WorkflowSpecialistOptionDto("specialist-2", "Priya Nair", ["service-9"]);
        var options = new BookingOptionsDto([MakeCustomer()], [MakeService()], [eligibleSpecialist, ineligibleSpecialist]);
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        sut.SelectedService = MakeService(); // Id = "service-1"

        var eligible = Assert.Single(sut.EligibleSpecialists);
        Assert.Equal("specialist-1", eligible.Id);
    }

    [Fact]
    public void SelectedService_Changed_RecomputesEligibleSpecialists()
    {
        var specialistForServiceA = new WorkflowSpecialistOptionDto("specialist-1", "Jordan Lee", ["service-a"]);
        var specialistForServiceB = new WorkflowSpecialistOptionDto("specialist-2", "Priya Nair", ["service-b"]);
        var serviceA = new WorkflowServiceOptionDto("service-a", "Service A", 60, "$10");
        var serviceB = new WorkflowServiceOptionDto("service-b", "Service B", 60, "$10");
        var options = new BookingOptionsDto([MakeCustomer()], [serviceA, serviceB], [specialistForServiceA, specialistForServiceB]);
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        sut.SelectedService = serviceA;
        Assert.Equal("specialist-1", Assert.Single(sut.EligibleSpecialists).Id);

        sut.SelectedService = serviceB;
        Assert.Equal("specialist-2", Assert.Single(sut.EligibleSpecialists).Id);
    }

    [Fact]
    public void SelectedService_NoEligibleSpecialists_SetsExplicitMessageInsteadOfSilentEmptyList()
    {
        var ineligibleSpecialist = new WorkflowSpecialistOptionDto("specialist-1", "Jordan Lee", ["service-9"]);
        var options = new BookingOptionsDto([MakeCustomer()], [MakeService()], [ineligibleSpecialist]);
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());

        sut.SelectedService = MakeService(); // Id = "service-1" - not in the specialist's list

        Assert.True(sut.HasNoEligibleSpecialists);
        Assert.False(string.IsNullOrEmpty(sut.NoEligibleSpecialistsMessage));
        Assert.Empty(sut.EligibleSpecialists);
    }

    [Fact]
    public void SelectedService_Changed_NeverMutatesUnderlyingSpecialistsCollection()
    {
        // Filtering is presentation-only - the source data (Specialists) is never touched, same
        // "no local corruption" proof shape as the Specialist Deactivation/Assignment tests.
        var options = MakeOptions();
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService());
        var originalCount = sut.Specialists.Count;

        sut.SelectedService = MakeService();

        Assert.Equal(originalCount, sut.Specialists.Count);
    }

    [Fact]
    public void SelectedService_ChangedToOneSelectedSpecialistIsNotEligibleFor_ClearsSelectedSpecialist()
    {
        // If Reception goes back and changes the service, a since-invalidated pairing must never
        // silently ride through to Date/TimeSlot.
        var specialistForServiceA = new WorkflowSpecialistOptionDto("specialist-1", "Jordan Lee", ["service-a"]);
        var serviceA = new WorkflowServiceOptionDto("service-a", "Service A", 60, "$10");
        var serviceB = new WorkflowServiceOptionDto("service-b", "Service B", 60, "$10");
        var options = new BookingOptionsDto([MakeCustomer()], [serviceA, serviceB], [specialistForServiceA]);
        var workflowService = new StubBookingWorkflowService(getOptions: _ => Task.FromResult(options));
        var sut = new BookingWizardViewModel(workflowService, new StubDialogService())
        {
            SelectedService = serviceA,
        };
        sut.SelectedSpecialist = specialistForServiceA;

        sut.SelectedService = serviceB;

        Assert.Null(sut.SelectedSpecialist);
    }
}
