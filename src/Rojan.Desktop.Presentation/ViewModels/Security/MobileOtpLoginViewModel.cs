using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
public sealed partial class MobileOtpLoginViewModel : ViewModelBase
{
    private static readonly Regex E164Pattern = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Login UI Simplification: Iranian mobile numbers as Persian users
    /// actually type them - a leading 0 (<c>09123456789</c>) or no prefix
    /// at all (<c>9123456789</c>) - both normalized to the E.164 form the
    /// backend's <c>POST /api/v1/auth/otp/request</c>/<c>otp/verify</c>
    /// always expected. Backend contract is unchanged; only what Desktop
    /// accepts as raw user input got more forgiving.
    /// </summary>
    private static readonly Regex IranLocalMobilePattern = new(@"^0?9\d{9}$", RegexOptions.Compiled);

    /// <summary>Persian-Indic and Extended Arabic-Indic digit characters, index-matched to '0'-'9' - both are commonly produced by a Persian keyboard layout.</summary>
    private const string PersianDigits = "۰۱۲۳۴۵۶۷۸۹";
    private const string ArabicIndicDigits = "٠١٢٣٤٥٦٧٨٩";

    private readonly IAuthenticationService _authenticationService;
    private readonly IDelayScheduler _delayScheduler;
    private readonly ILogger<MobileOtpLoginViewModel> _logger;

    private string _phoneNumber = string.Empty;
    private string _code = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCodeSent;
    private bool _canResend;
    private IDisposable? _resendCooldownHandle;

    public MobileOtpLoginViewModel(IAuthenticationService authenticationService, IDelayScheduler delayScheduler, ILogger<MobileOtpLoginViewModel>? logger = null)
    {
        _authenticationService = authenticationService;
        _delayScheduler = delayScheduler;
        _logger = logger ?? NullLogger<MobileOtpLoginViewModel>.Instance;
        RequestCodeCommand = new AsyncRelayCommand(_ => RequestCodeAsync(), _ => CanRequestCode());
        ResendCodeCommand = new AsyncRelayCommand(_ => ResendCodeAsync(), _ => CanResendCode());
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

    /// <summary>
    /// Login UI Simplification: converts Persian-Indic/Arabic-Indic digits to ASCII and strips
    /// spaces/dashes/parentheses - the one cleanup step both <see cref="NormalizePhoneNumber"/>
    /// and <see cref="ClassifyInvalidPhoneNumber"/> (Iranian Phone Input UX Fix) share, so a
    /// validation message is never wrong just because of how the number was punctuated. A
    /// leading <c>+</c> and every digit character pass through unchanged; anything else (a
    /// letter, for example) is left in place too - callers decide what to do with it.
    /// </summary>
    private static string CleanDigits(string input)
    {
        var digits = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            if (ch is ' ' or '-' or '(' or ')')
            {
                continue;
            }

            var persianIndex = PersianDigits.IndexOf(ch);
            if (persianIndex >= 0)
            {
                digits.Append((char)('0' + persianIndex));
                continue;
            }

            var arabicIndex = ArabicIndicDigits.IndexOf(ch);
            if (arabicIndex >= 0)
            {
                digits.Append((char)('0' + arabicIndex));
                continue;
            }

            digits.Append(ch);
        }

        return digits.ToString();
    }

    /// <summary>
    /// Login UI Simplification: expands a local Iranian mobile number (<see cref="IranLocalMobilePattern"/>)
    /// or a <c>0098</c>-prefixed one to E.164 - already-E.164 input passes through unchanged.
    /// Purely a display/input-tolerance concern; the normalized value is what every caller
    /// downstream (validation, the API call) actually uses. Backend contract is unchanged - only
    /// what Desktop accepts as raw user input got more forgiving.
    /// </summary>
    private static string NormalizePhoneNumber(string input)
    {
        var normalized = CleanDigits(input);

        if (normalized.StartsWith("0098", StringComparison.Ordinal))
        {
            normalized = "+98" + normalized[4..];
        }
        else if (IranLocalMobilePattern.IsMatch(normalized))
        {
            normalized = normalized.StartsWith('0') ? "+98" + normalized[1..] : "+98" + normalized;
        }

        return normalized;
    }

    /// <summary>
    /// Iranian Phone Input UX Fix: classifies *why* a raw (cleaned, but not yet normalized) phone
    /// number will fail E.164 validation, so the login screen can show a specific, actionable
    /// message instead of one generic "invalid phone" for every case. Only classifies the local
    /// Iranian input shapes <see cref="NormalizePhoneNumber"/> itself understands (a bare
    /// <c>+...</c> international number falls through to the existing generic message, unchanged -
    /// this fix's scope is Iranian local input, not general E.164 diagnostics). Returns
    /// <see langword="null"/> when it finds nothing specific to say, in which case the caller
    /// falls back to <see cref="Strings.Login_Mobile_Error_InvalidPhone"/> exactly as before this
    /// fix.
    /// </summary>
    private static string? ClassifyInvalidPhoneNumber(string cleaned)
    {
        if (cleaned.Length == 0 || cleaned == "+")
        {
            return null;
        }

        if (cleaned.StartsWith('+') || cleaned.StartsWith("0098", StringComparison.Ordinal))
        {
            // Already-international shapes are NormalizePhoneNumber's/E164Pattern's own
            // territory - not this fix's Iranian-local-input scope.
            return null;
        }

        if (!cleaned.All(char.IsAsciiDigit))
        {
            return Strings.Login_Mobile_Error_InvalidPhoneCharacters;
        }

        var startsWithNine = cleaned[0] == '9';
        var startsWithZeroNine = cleaned.Length > 1 && cleaned[0] == '0' && cleaned[1] == '9';
        if (!startsWithNine && !startsWithZeroNine)
        {
            return Strings.Login_Mobile_Error_WrongPrefix;
        }

        var localLength = cleaned[0] == '0' ? cleaned.Length - 1 : cleaned.Length;
        return localLength < 10 ? Strings.Login_Mobile_Error_TooShort : null;
    }

    private async Task RequestCodeAsync()
    {
        var phoneNumber = NormalizePhoneNumber(PhoneNumber);
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            ErrorMessage = Strings.Login_Mobile_Error_MissingPhone;
            return;
        }

        if (!E164Pattern.IsMatch(phoneNumber))
        {
            // Iranian Phone Input UX Fix: a specific reason (too short / invalid characters /
            // wrong prefix) when one is identifiable, the same generic message as before
            // otherwise - see ClassifyInvalidPhoneNumber's own doc comment for its scope.
            ErrorMessage = ClassifyInvalidPhoneNumber(CleanDigits(PhoneNumber)) ?? Strings.Login_Mobile_Error_InvalidPhone;
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var challenge = await _authenticationService.RequestOtpAsync(phoneNumber).ConfigureAwait(true);
            ApplyIssuedChallenge(challenge);
        }
        catch (ApiRateLimitException)
        {
            // Desktop OTP Authentication Migration: OTP_REQUEST_RATE_LIMITED -
            // the real backend's request/resend rate limiter (shared between
            // the two endpoints; see AuthController.kt's "resendOtp ...
            // subject to the same rate limits as /otp/request").
            ErrorMessage = Strings.Login_Mobile_Error_RateLimited;
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
            LogUnexpectedOtpApiFailure(nameof(RequestCodeAsync));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Desktop OTP Authentication Migration: re-issues a code via the real
    /// backend's distinct <c>/otp/resend</c> endpoint (see
    /// <see cref="IAuthenticationService.ResendOtpAsync"/>'s own doc
    /// comment) - previously this reused <see cref="RequestCodeAsync"/>
    /// (and therefore <c>/otp/request</c>) for both first-send and resend,
    /// which the real backend contract does not support as the same call.
    /// </summary>
    private async Task ResendCodeAsync()
    {
        var phoneNumber = NormalizePhoneNumber(PhoneNumber);

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var challenge = await _authenticationService.ResendOtpAsync(phoneNumber).ConfigureAwait(true);
            ApplyIssuedChallenge(challenge);
        }
        catch (ApiRateLimitException)
        {
            ErrorMessage = Strings.Login_Mobile_Error_RateLimited;
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
            LogUnexpectedOtpApiFailure(nameof(ResendCodeAsync));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyIssuedChallenge(OtpChallenge challenge)
    {
        IsCodeSent = true;
        CanResend = false;
        _resendCooldownHandle?.Dispose();
        _resendCooldownHandle = _delayScheduler.Schedule(challenge.CanResendAfter, () =>
        {
            CanResend = true;
            CommandManager.InvalidateRequerySuggested();
        });
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
            await _authenticationService.SignInWithOtpAsync(NormalizePhoneNumber(PhoneNumber), Code.Trim()).ConfigureAwait(true);
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiAuthenticationException exception)
        {
            // Desktop OTP Authentication Migration: confirmed against the
            // real backend's GlobalExceptionHandler/OtpDomainExceptions - a
            // 403 here is always INACTIVE_USER (the account is deactivated/
            // blocked), never a generic authorization rejection; a 401 is
            // always INVALID_OTP, which the real backend deliberately
            // collapses "wrong code," "expired code," and "no active code
            // at all" into one indistinguishable case (avoids an
            // enumeration/timing signal) - so this can only ever show one
            // message for all three, by backend design, not a client
            // limitation.
            ErrorMessage = exception.StatusCode == 403
                ? Strings.Login_Mobile_Error_NotAuthorized
                : Strings.Login_Mobile_Error_InvalidCode;
        }
        catch (ApiRateLimitException)
        {
            // OTP_VERIFY_RATE_LIMITED - the real backend's per-phone verify-attempt
            // rate limiter, distinct from the request/resend one above.
            ErrorMessage = Strings.Login_Mobile_Error_RateLimited;
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
            LogUnexpectedOtpApiFailure(nameof(VerifyCodeAsync));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // the phone number, the OTP code, or any token/session data. The OTP client
    // (AuthBootstrapHttpClient) embeds the raw backend response body in
    // ApiException messages, so the exception is deliberately not passed here.
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "OTP API request failed during {Operation}")]
    private partial void LogUnexpectedOtpApiFailure(string operation);
}
