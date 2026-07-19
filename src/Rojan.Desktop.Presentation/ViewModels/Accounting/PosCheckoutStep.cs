namespace Rojan.Desktop.Presentation.ViewModels.Accounting;

/// <summary>
/// The POS checkout's linear steps. "Payment dialog" (one of Phase 18's
/// explicit deliverables) is realized as the Payment step within this
/// wizard-dialog rather than a separately-launched nested dialog - Shell's
/// dialog region (<c>MainWindowViewModel.ActiveDialog</c>) only supports
/// one active dialog at a time, the same constraint
/// <c>BookingWorkflow.BookingWizardViewModel</c> works within for its own
/// Review/Confirmation steps.
/// </summary>
public enum PosCheckoutStep
{
    Cart,
    Payment,
    Receipt,
}
