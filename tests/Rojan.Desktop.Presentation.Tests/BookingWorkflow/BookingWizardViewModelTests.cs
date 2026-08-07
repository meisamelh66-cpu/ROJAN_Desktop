using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.BookingWorkflow;

public sealed class BookingWizardViewModelTests
{
    private static readonly DateTimeOffset SlotStart = new(2026, 3, 2, 9, 0, 0, DateTimeOffset.Now.Offset);

    private static WorkflowCustomerOptionDto MakeCustomer() => new("customer-1", "Amelia Hart");

    private static WorkflowServiceOptionDto MakeService() => new("service-1", "Haircut & Style", 60, "$65");

    private static WorkflowSpecialistOptionDto MakeSpecialist() => new("specialist-1", "Jordan Lee");

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
}
