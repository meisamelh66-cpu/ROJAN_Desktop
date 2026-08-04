using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Owner App Mobile Login: composes the primary Mobile Number + OTP flow
/// (<see cref="MobileLogin"/>) with the still-fully-functional email/
/// password flow (<see cref="EmailLogin"/>, kept as a secondary/fallback
/// path per "remove dependency on email login as primary UI flow, keep
/// email support internally") behind one <see cref="SignedIn"/> signal -
/// <c>LoginWindow</c>'s actual DataContext now. <see cref="IsEmailModeActive"/>
/// is the one flag its XAML needs to switch between the two panels; neither
/// child ViewModel knows the other exists, so <see cref="LoginViewModel"/>
/// itself needed zero changes for this - it is exactly as testable and
/// self-contained as it was before this flow existed.
/// </summary>
public sealed class LoginWindowViewModel : ViewModelBase
{
    private bool _isEmailModeActive;

    public LoginWindowViewModel(MobileOtpLoginViewModel mobileLogin, LoginViewModel emailLogin)
    {
        MobileLogin = mobileLogin;
        EmailLogin = emailLogin;
        SwitchToEmailLoginCommand = new RelayCommand(_ => IsEmailModeActive = true);
        SwitchToMobileLoginCommand = new RelayCommand(_ => IsEmailModeActive = false);
        MobileLogin.SignedIn += OnChildSignedIn;
        EmailLogin.SignedIn += OnChildSignedIn;
    }

    public MobileOtpLoginViewModel MobileLogin { get; }

    public LoginViewModel EmailLogin { get; }

    /// <summary>False (Mobile Number + OTP, the default/primary flow) unless the user explicitly asked to fall back to email/password.</summary>
    public bool IsEmailModeActive
    {
        get => _isEmailModeActive;
        private set => SetProperty(ref _isEmailModeActive, value);
    }

    public ICommand SwitchToEmailLoginCommand { get; }

    public ICommand SwitchToMobileLoginCommand { get; }

    /// <summary>Raised once either child ViewModel's own <c>SignedIn</c> fires - <c>LoginWindow</c> subscribes to this one event regardless of which flow the user completed.</summary>
    public event EventHandler? SignedIn;

    private void OnChildSignedIn(object? sender, EventArgs e) => SignedIn?.Invoke(this, e);
}
