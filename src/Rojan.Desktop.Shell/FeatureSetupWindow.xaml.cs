using System.Windows;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Features;

namespace Rojan.Desktop.Shell;

/// <summary>
/// PASS D10 (Feature Personalization &amp; Modular Workspace): a standalone top-level
/// <see cref="Window"/>, same shape as <see cref="SalonSelectionWindow"/> and for the same reason (see
/// that class's own doc comment) - shown, and can succeed, entirely before
/// <c>MainWindow</c>/<c>MainWindowViewModel</c> exist, from <c>App.xaml.cs</c>'s <c>OnStartup</c>
/// gating, right after the salon-resolution gate. Constructed via <c>new</c> (not DI-resolved) since
/// its ViewModel needs the real, already-resolved module list - see
/// <see cref="FeatureSetupWindowViewModel"/>'s own doc comment.
/// </summary>
public partial class FeatureSetupWindow : Window
{
    public FeatureSetupWindow(FeatureSetupWindowViewModel viewModel, ICultureService cultureService, ILocalizationService localizationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        FlowDirection = cultureService.GetFlowDirection(localizationService.CurrentLanguage.IsRightToLeft);
        viewModel.Completed += OnCompleted;
        Closed += (_, _) => viewModel.Completed -= OnCompleted;
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        DialogResult = true;
    }
}
