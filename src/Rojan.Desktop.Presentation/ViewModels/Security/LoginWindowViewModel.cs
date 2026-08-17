using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Desktop OTP Authentication Migration: thin wrapper around the Phone
/// Number + OTP flow (<see cref="MobileLogin"/>) - <c>LoginWindow</c>'s
/// actual DataContext. Mobile Number + OTP was previously reverted to
/// Email/Password (Authentication Recovery, Option C) because the real
/// ROJAN_Backend had no OTP endpoints at the time - see
/// ROJAN_Authentication_Contract_Verification.md/
/// ROJAN_Authentication_Recovery_Plan.md. That contract is now confirmed
/// live (v1.0.2-production-release: <c>POST /auth/otp/request</c>/
/// <c>/resend</c>/<c>/verify</c>, verified field-for-field against the real
/// backend source), so Phone + OTP is restored as the sole sign-in flow per
/// the approved Identity Policy - Email/Password (<see cref="LoginViewModel"/>)
/// is deliberately left completely untouched (not deleted, not modified),
/// matching this repo's own "superseded but not deleted" convention
/// (<c>Infrastructure.Security.LocalAuthenticationService</c>), in case it
/// is ever needed again. This wrapper is kept (rather than
/// binding <c>LoginWindow</c> directly to <see cref="MobileOtpLoginViewModel"/>)
/// so <c>SignedIn</c> keeps the same event shape/name the Shell's DI wiring
/// and <c>LoginWindow.xaml.cs</c> already depend on.
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
