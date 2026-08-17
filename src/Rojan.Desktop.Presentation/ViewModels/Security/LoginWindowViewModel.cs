using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Login UI Simplification: thin wrapper around the primary Mobile Number +
/// OTP flow (<see cref="MobileLogin"/>) - <c>LoginWindow</c>'s actual
/// DataContext. Previously also composed a secondary Email/Password flow
/// (<see cref="LoginViewModel"/>) behind a mode-switch flag; that flow was
/// removed from the Login UI entirely (backend/API support for it is
/// unchanged and still exists in <c>IAuthenticationService</c>/
/// <see cref="LoginViewModel"/> itself, simply no longer wired into this
/// window) so Mobile Number + OTP is now the only way to sign in from
/// Desktop. This wrapper is kept (rather than binding <c>LoginWindow</c>
/// directly to <see cref="MobileOtpLoginViewModel"/>) so <c>SignedIn</c>
/// keeps the same event shape/name the Shell's DI wiring and
/// <c>LoginWindow.xaml.cs</c> already depend on.
/// </summary>
public sealed class LoginWindowViewModel : ViewModelBase
{
    public LoginWindowViewModel(MobileOtpLoginViewModel mobileLogin)
    {
        MobileLogin = mobileLogin;
        MobileLogin.SignedIn += OnMobileSignedIn;
    }

    public MobileOtpLoginViewModel MobileLogin { get; }

    /// <summary>Raised once <see cref="MobileLogin"/>'s own <c>SignedIn</c> fires - <c>LoginWindow</c> subscribes to this to know when to close.</summary>
    public event EventHandler? SignedIn;

    private void OnMobileSignedIn(object? sender, EventArgs e) => SignedIn?.Invoke(this, e);
}
