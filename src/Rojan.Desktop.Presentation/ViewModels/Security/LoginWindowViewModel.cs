using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Authentication Recovery (Option C): thin wrapper around the Email/
/// Password flow (<see cref="EmailLogin"/>) - <c>LoginWindow</c>'s actual
/// DataContext. Mobile Number + OTP (<see cref="MobileOtpLoginViewModel"/>)
/// was the sole sign-in flow for a period, but calls backend endpoints
/// (<c>/auth/otp/request</c>/<c>/verify</c>) that do not exist on the real,
/// confirmed-authoritative ROJAN_Backend - see
/// ROJAN_Authentication_Contract_Verification.md/
/// ROJAN_Authentication_Recovery_Plan.md. Email/Password calls endpoints
/// that are verified to exist and match exactly, so it is restored here as
/// the only wired flow; <see cref="MobileOtpLoginViewModel"/> itself is
/// deliberately left completely untouched (not deleted, not modified) so
/// future OTP work - once ROJAN_Backend actually supports it - can re-wire
/// it without rebuilding it. This wrapper is kept (rather than binding
/// <c>LoginWindow</c> directly to <see cref="LoginViewModel"/>) so
/// <c>SignedIn</c> keeps the same event shape/name the Shell's DI wiring
/// and <c>LoginWindow.xaml.cs</c> already depend on.
/// </summary>
public sealed class LoginWindowViewModel : ViewModelBase
{
    public LoginWindowViewModel(LoginViewModel emailLogin)
    {
        EmailLogin = emailLogin;
        EmailLogin.SignedIn += OnEmailSignedIn;
    }

    public LoginViewModel EmailLogin { get; }

    /// <summary>Raised once <see cref="EmailLogin"/>'s own <c>SignedIn</c> fires - <c>LoginWindow</c> subscribes to this to know when to close.</summary>
    public event EventHandler? SignedIn;

    private void OnEmailSignedIn(object? sender, EventArgs e) => SignedIn?.Invoke(this, e);
}
