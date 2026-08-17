using System.Windows;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Desktop OTP Authentication Migration: Phone Number + OTP is the sign-in
/// flow (see <see cref="LoginWindowViewModel"/>'s own doc comment) - no
/// code-behind is needed for it beyond DataContext wiring, since every
/// input (<c>PhoneNumber</c>, <c>Code</c>) is a plain, bindable string
/// property, unlike the removed Email/Password flow's <c>PasswordBox</c>
/// (which WPF deliberately does not expose as a bindable
/// <c>DependencyProperty</c>). <see cref="LoginWindowViewModel.SignedIn"/>
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
