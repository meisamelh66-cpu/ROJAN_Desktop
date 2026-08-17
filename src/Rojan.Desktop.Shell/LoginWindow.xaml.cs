using System.Windows;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Login UI Simplification: Mobile Number + OTP is now the only sign-in
/// flow (see <see cref="LoginWindowViewModel"/>'s own doc comment) - no
/// code-behind is needed for it beyond DataContext/FlowDirection setup,
/// since <c>PhoneNumber</c>/<c>Code</c> are plain bindable
/// <c>TextBox</c> values. <see cref="LoginWindowViewModel.SignedIn"/>
/// closes this window with <c>DialogResult = true</c>; closing via the
/// window chrome (X button) instead leaves <c>DialogResult</c>
/// <see langword="null"/> - see <c>App.xaml.cs</c>'s OnStartup gating for
/// how each case is handled.
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow(LoginWindowViewModel viewModel, ICultureService cultureService, ILocalizationService localizationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        FlowDirection = cultureService.GetFlowDirection(localizationService.CurrentLanguage.IsRightToLeft);
        viewModel.SignedIn += OnSignedIn;
        Closed += (_, _) => viewModel.SignedIn -= OnSignedIn;
    }

    private void OnSignedIn(object? sender, EventArgs e)
    {
        DialogResult = true;
    }
}
