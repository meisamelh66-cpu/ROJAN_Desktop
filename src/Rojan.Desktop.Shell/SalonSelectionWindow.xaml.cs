using System.Windows;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Salons;

namespace Rojan.Desktop.Shell;

/// <summary>
/// PASS D6 (Active Salon Context Correctness): a standalone top-level <see cref="Window"/>, same
/// shape as <see cref="LoginWindow"/> and for the same reason (see that class's own doc comment) -
/// shown, and can succeed, entirely before <c>MainWindow</c>/<c>MainWindowViewModel</c> exist, from
/// <c>App.xaml.cs</c>'s <c>OnStartup</c> gating, right after the login gate. Constructed via
/// <c>new</c> (not DI-resolved) since its ViewModel needs the real, already-resolved candidate list
/// - see <see cref="SalonSelectionWindowViewModel"/>'s own doc comment.
/// </summary>
public partial class SalonSelectionWindow : Window
{
    public SalonSelectionWindow(SalonSelectionWindowViewModel viewModel, ICultureService cultureService, ILocalizationService localizationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        FlowDirection = cultureService.GetFlowDirection(localizationService.CurrentLanguage.IsRightToLeft);
        viewModel.Selected += OnSelected;
        Closed += (_, _) => viewModel.Selected -= OnSelected;
    }

    private void OnSelected(object? sender, EventArgs e)
    {
        DialogResult = true;
    }
}
