namespace Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;

/// <summary>
/// The Booking Wizard's linear steps. Customer selection is not one of
/// the six steps Phase 15 explicitly enumerates (Service/Specialist/Date/
/// TimeSlot/Review/Confirmation), but "coordinate Customer" in that same
/// requirement list only means something if the wizard actually picks a
/// real customer rather than free text - so it is added here as the
/// natural first step, ahead of Service selection.
/// </summary>
public enum BookingWizardStep
{
    Customer,
    Service,
    Specialist,
    Date,
    TimeSlot,
    Review,
    Confirmation,
}
