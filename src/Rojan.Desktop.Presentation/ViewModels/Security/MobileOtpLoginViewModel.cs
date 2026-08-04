using System.Text.RegularExpressions;
using System.Windows.Input;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Threading;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Owner App Mobile Login: drives the primary Login screen flow (Mobile
/// Number -&gt; Request OTP -&gt; OTP Input -&gt; Verify OTP -&gt; AuthResponse -&gt;
/// JWT Secure Storage -&gt; Dashboard). Both steps live in one ViewModel
/// (not two) because they share too much state - <see cref="PhoneNumber"/>,
/// <see cref="ErrorMessage"/>, <see cref="IsBusy"/> - to be worth splitting;
/// <see cref="IsCodeSent"/> is the one flag <c>LoginWindow</c>'s XAML needs
/// to switch between the phone-entry and code-entry panels. Mirrors
/// <see cref="LoginViewModel"/>'s "set <see cref="ErrorMessage"/> directly,
/// never a dialog" convention and its <c>SignedIn</c> signal shape exactly -
/// <see cref="LoginWindowViewModel"/> treats both the same way.
/// </summary>
public sealed class MobileOtpLoginViewModel : ViewModelBase
{
    private static readonly Regex E164Pattern = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    private readonly IAuthenticationService _authenticationService;
    private readonly IDelayScheduler _delayScheduler;

    private string _phoneNumber = string.Empty;
    private string _code = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCodeSent;
    private bool _canResend;
    private IDisposable? _resendCooldownHandle;

    public MobileOtpLoginViewModel(IAuthenticationService authenticationService, IDelayScheduler delayScheduler)
    {
        _authenticationService = authenticationService;
        _delayScheduler = delayScheduler;
        RequestCodeCommand = new AsyncRelayCommand(_ => RequestCodeAsync(), _ => CanRequestCode());
        ResendCodeCommand = new AsyncRelayCommand(_ => RequestCodeAsync(), _ => CanResendCode());
        VerifyCodeCommand = new AsyncRelayCommand(_ => VerifyCodeAsync(), _ => CanVerifyCode());
        ChangeNumberCommand = new RelayCommand(_ => ChangeNumber(), _ => !IsBusy);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (SetProperty(ref _phoneNumber, value))
            {
                ErrorMessage = null;
            }
        }
    }

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value))
            {
                ErrorMessage = null;
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>True once a code has been sent - the trigger for switching from the phone-entry panel to the code-entry panel.</summary>
    public bool IsCodeSent
    {
        get => _isCodeSent;
        private set => SetProperty(ref _isCodeSent, value);
    }

    /// <summary>False immediately after a code is (re)sent, until the backend's own resend cooldown (<see cref="OtpChallenge.CanResendAfter"/>) elapses.</summary>
    public bool CanResend
    {
        get => _canResend;
        private set => SetProperty(ref _canResend, value);
    }

    public ICommand RequestCodeCommand { get; }

    public ICommand ResendCodeCommand { get; }

    public ICommand VerifyCodeCommand { get; }

    public ICommand ChangeNumberCommand { get; }

    /// <summary>Raised once sign-in actually succeeds - same contract as <see cref="LoginViewModel.SignedIn"/>.</summary>
    public event EventHandler? SignedIn;

    private bool CanRequestCode() => !IsBusy && !IsCodeSent;

    private bool CanResendCode() => !IsBusy && IsCodeSent && CanResend;

    private bool CanVerifyCode() => !IsBusy && IsCodeSent;

    private void ChangeNumber()
    {
        if (IsBusy)
        {
            return;
        }

        _resendCooldownHandle?.Dispose();
        _resendCooldownHandle = null;
        IsCodeSent = false;
        CanResend = false;
        Code = string.Empty;
        ErrorMessage = null;
    }

    private async Task RequestCodeAsync()
    {
        var phoneNumber = PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            ErrorMessage = Strings.Login_Mobile_Error_MissingPhone;
            return;
        }

        if (!E164Pattern.IsMatch(phoneNumber))
        {
            ErrorMessage = Strings.Login_Mobile_Error_InvalidPhone;
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var challenge = await _authenticationService.RequestOtpAsync(phoneNumber).ConfigureAwait(true);
            IsCodeSent = true;
            CanResend = false;
            _resendCooldownHandle?.Dispose();
            _resendCooldownHandle = _delayScheduler.Schedule(challenge.CanResendAfter, () =>
            {
                CanResend = true;
                CommandManager.InvalidateRequerySuggested();
            });
        }
        catch (ApiConnectivityException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiTimeoutException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiException)
        {
            ErrorMessage = Strings.Login_Error_Generic;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task VerifyCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = Strings.Login_Mobile_Error_MissingCode;
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _authenticationService.SignInWithOtpAsync(PhoneNumber.Trim(), Code.Trim()).ConfigureAwait(true);
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiAuthenticationException)
        {
            ErrorMessage = Strings.Login_Mobile_Error_InvalidCode;
        }
        catch (ApiConnectivityException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiTimeoutException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiException)
        {
            ErrorMessage = Strings.Login_Error_Generic;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
