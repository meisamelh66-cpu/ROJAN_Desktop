namespace Rojan.Desktop.Presentation.Dialogs;

/// <summary>
/// Presentation-owned abstraction over Shell's dialog region - same
/// pattern as <c>Navigation.INavigationService</c>: the interface lives
/// here, the concrete implementation lives in Shell (backed by
/// <c>MainWindowViewModel.ActiveDialog</c>, an extension point that has
/// existed unused since Phase 07's own doc comment on that property said
/// "a future IDialogService sets this"). Presentation ViewModels depend
/// only on this interface, never on Shell directly.
/// </summary>
public interface IDialogService
{
    /// <summary>Shows <paramref name="viewModel"/> in the dialog region - resolved to a View via the same implicit DataTemplate mechanism page navigation already uses.</summary>
    public void ShowDialog(object viewModel);

    /// <summary>Hides whatever dialog is currently shown, if any.</summary>
    public void CloseDialog();
}
