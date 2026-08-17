using System.Windows;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Authentication Recovery (Option C): Email/Password is the sign-in flow
/// again (see <see cref="LoginWindowViewModel"/>'s own doc comment).
/// <see cref="PasswordBox_PasswordChanged"/> is the one necessary piece of
/// code-behind: WPF's <c>PasswordBox.Password</c> is deliberately not a
/// bindable <c>DependencyProperty</c> (so a plaintext password never
/// round-trips through the data-binding engine/visual tree), so forwarding
/// it to <c>LoginWindowViewModel.EmailLogin.Password</c> on every keystroke
/// is the standard, only supported way to connect a <c>PasswordBox</c> to
/// MVVM. <see cref="LoginWindowViewModel.SignedIn"/> closes this window
/// with <c>DialogResult = true</c>; closing via the window chrome (X
/// button) instead leaves <c>DialogResult</c> <see langword="null"/> - see
/// <c>App.xaml.cs</c>'s OnStartup gating for how each case is handled.
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

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginWindowViewModel viewModel)
        {
            viewModel.EmailLogin.Password = PasswordBox.Password;
        }
    }
}
