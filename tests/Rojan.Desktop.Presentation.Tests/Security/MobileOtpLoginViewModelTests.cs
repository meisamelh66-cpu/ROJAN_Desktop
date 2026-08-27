using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Exercises <see cref="MobileOtpLoginViewModel"/> - the primary Mobile Number + OTP flow (request/resend/verify, validation, and error mapping), mirroring <see cref="LoginViewModelTests"/>'s coverage shape for the email flow it now sits alongside.</summary>
public sealed class MobileOtpLoginViewModelTests
{
    [Fact]
    public async Task RequestCodeCommand_MissingPhoneNumber_ShowsInlineErrorWithoutCallingTheService()
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = string.Empty };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_MissingPhone, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    [Theory]
    [InlineData("+98")]
    public async Task RequestCodeCommand_InvalidPhoneNumberFormat_ShowsInlineErrorWithoutCallingTheService(string phoneNumber)
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_InvalidPhone, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    /// <summary>
    /// Iranian Phone Input UX Fix: a specific, actionable message per failure reason instead of
    /// one generic "invalid phone" for every case - see <c>MobileOtpLoginViewModel.ClassifyInvalidPhoneNumber</c>'s
    /// own doc comment. "091234567" is a real Iranian local number one digit short of the real
    /// 10-digit subscriber length (11 with the leading 0) - previously indistinguishable from
    /// "not-a-phone-number" (both showed the same generic message); now a distinct "too short"
    /// message.
    /// </summary>
    [Theory]
    [InlineData("091234567")] // 9 digits after the leading 0 - one short of a real Iranian number
    [InlineData("09123456")] // even shorter
    [InlineData("9123456")] // short, no leading 0
    public async Task RequestCodeCommand_PhoneNumberTooShort_ShowsTooShortMessage(string phoneNumber)
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_TooShort, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    [Theory]
    [InlineData("not-a-phone-number")]
    [InlineData("09123abc789")]
    [InlineData("0912345678a")]
    public async Task RequestCodeCommand_PhoneNumberHasInvalidCharacters_ShowsInvalidCharactersMessage(string phoneNumber)
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_InvalidPhoneCharacters, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    [Theory]
    [InlineData("08123456789")] // wrong second digit - not a mobile prefix
    [InlineData("01123456789")]
    [InlineData("11234567890")]
    public async Task RequestCodeCommand_PhoneNumberHasWrongPrefix_ShowsWrongPrefixMessage(string phoneNumber)
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_WrongPrefix, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    /// <summary>
    /// Login UI Simplification: Iranian-local and Persian-digit input, all normalized to
    /// the same E.164 value the backend always expected - see <c>MobileOtpLoginViewModel.NormalizePhoneNumber</c>'s
    /// own doc comment.
    /// </summary>
    [Theory]
    [InlineData("09123456789", "+989123456789")] // local, leading 0
    [InlineData("9123456789", "+989123456789")] // local, no leading 0
    [InlineData("00989123456789", "+989123456789")] // 00-prefixed international
    [InlineData("0912 345 6789", "+989123456789")] // spaces
    [InlineData("0912-345-6789", "+989123456789")] // dashes
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹", "+989123456789")] // Persian-Indic digits
    [InlineData("+989123456789", "+989123456789")] // already E.164 - passes through unchanged
    public async Task RequestCodeCommand_LocalOrPersianFormattedPhoneNumber_NormalizesAndCallsTheServiceWithE164(string phoneNumber, string expectedE164)
    {
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge(expectedE164, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Null(sut.ErrorMessage);
        Assert.True(sut.IsCodeSent);
        Assert.Equal(1, service.RequestOtpCallCount);
        Assert.Equal(expectedE164, service.LastPhoneNumber);
    }

    /// <summary>
    /// Iranian Phone Input UX Fix: the exact scenario the mission brief specified - a leading-0
    /// local Iranian number normalizes to +98, unchanged, before reaching the API call.
    /// </summary>
    [Fact]
    public async Task RequestCodeCommand_LeadingZeroLocalNumber_NormalizesToPlus98BeforeCallingTheService()
    {
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge("+989164875850", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "09164875850" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, service.RequestOtpCallCount);
        Assert.Equal("+989164875850", service.LastPhoneNumber);
    }

    /// <summary>
    /// Iranian Phone Input UX Fix: the mission brief's own literal example, "0916487585" -&gt;
    /// "+98916487585" - is one digit short of a real Iranian mobile number (a real subscriber
    /// number is 10 digits after the leading 0, e.g. "0916487585" + one more digit; the brief's
    /// example has only 9). Documented here rather than silently "fixed" by loosening the
    /// existing, already-correct 10-digit local-number pattern (<c>IranLocalMobilePattern</c>,
    /// unchanged by this fix) to accept 9-digit numbers as if they were valid - that would let a
    /// genuinely incomplete number reach the backend as a well-formed (but wrong) E.164 value
    /// instead of being caught here. The correct, honest outcome for this exact input is the new
    /// "too short" message (see <see cref="RequestCodeCommand_PhoneNumberTooShort_ShowsTooShortMessage"/>
    /// for the same case, asserted directly) - not a silent normalization to an
    /// under-length number.
    /// </summary>
    [Fact]
    public async Task RequestCodeCommand_MissionBriefLiteralExample_IsOneDigitShortAndCorrectlyRejected()
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "0916487585" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_TooShort, sut.ErrorMessage);
        Assert.Equal(0, service.RequestOtpCallCount);
    }

    [Fact]
    public async Task RequestCodeCommand_Success_SwitchesToCodeStepAndSchedulesTheResendCooldown()
    {
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var scheduler = new StubDelayScheduler();
        var sut = new MobileOtpLoginViewModel(service, scheduler) { PhoneNumber = "+989123456789" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.True(sut.IsCodeSent);
        Assert.False(sut.CanResend);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, service.RequestOtpCallCount);
        Assert.Equal("+989123456789", service.LastPhoneNumber);
        Assert.Single(scheduler.ScheduledCallbacks);
    }

    [Fact]
    public async Task RequestCodeCommand_ResendCooldownElapses_ReEnablesResend()
    {
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var scheduler = new StubDelayScheduler();
        var sut = new MobileOtpLoginViewModel(service, scheduler) { PhoneNumber = "+989123456789" };
        await ExecuteAsync(sut.RequestCodeCommand, sut);

        scheduler.FireAll();

        Assert.True(sut.CanResend);
    }

    [Theory]
    [InlineData(typeof(ApiConnectivityException))]
    [InlineData(typeof(ApiTimeoutException))]
    public async Task RequestCodeCommand_NetworkFailure_ShowsNetworkErrorMessage(Type exceptionType)
    {
        var exception = (ApiException)Activator.CreateInstance(exceptionType, "network down")!;
        var service = new StubAuthenticationService { RequestOtpExceptionToThrow = exception };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Error_Network, sut.ErrorMessage);
        Assert.False(sut.IsCodeSent);
    }

    [Fact]
    public async Task RequestCodeCommand_RejectedByTheBackend_ShowsGenericErrorMessage()
    {
        // A non-2xx, non-rate-limited, non-auth rejection (e.g. a validation
        // failure) still surfaces the same generic message.
        var service = new StubAuthenticationService { RequestOtpExceptionToThrow = new ApiException("Malformed request") };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Error_Generic, sut.ErrorMessage);
        Assert.False(sut.IsCodeSent);
    }

    [Fact]
    public async Task RequestCodeCommand_RateLimited_ShowsRateLimitedMessage()
    {
        // Desktop OTP Authentication Migration: real backend's OTP_REQUEST_RATE_LIMITED (429).
        var service = new StubAuthenticationService { RequestOtpExceptionToThrow = new ApiRateLimitException("429") };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_RateLimited, sut.ErrorMessage);
        Assert.False(sut.IsCodeSent);
    }

    [Fact]
    public async Task ResendCodeCommand_Success_CallsResendOtpNotRequestOtpAndReArmsTheCooldown()
    {
        // Desktop OTP Authentication Migration: resend hits the real backend's distinct
        // /otp/resend endpoint, not /otp/request again.
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var scheduler = new StubDelayScheduler();
        var sut = new MobileOtpLoginViewModel(service, scheduler) { PhoneNumber = "+989123456789" };
        await ExecuteAsync(sut.RequestCodeCommand, sut);
        scheduler.FireAll();
        Assert.True(sut.CanResend);

        await ExecuteAsync(sut.ResendCodeCommand, sut);

        Assert.Equal(1, service.RequestOtpCallCount);
        Assert.Equal(1, service.ResendOtpCallCount);
        Assert.True(sut.IsCodeSent);
        Assert.False(sut.CanResend);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task ResendCodeCommand_RateLimited_ShowsRateLimitedMessage()
    {
        var service = new StubAuthenticationService
        {
            ChallengeToReturn = new OtpChallenge("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)),
        };
        var scheduler = new StubDelayScheduler();
        var sut = new MobileOtpLoginViewModel(service, scheduler) { PhoneNumber = "+989123456789" };
        await ExecuteAsync(sut.RequestCodeCommand, sut);
        scheduler.FireAll();
        service.ResendOtpExceptionToThrow = new ApiRateLimitException("429");

        await ExecuteAsync(sut.ResendCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_RateLimited, sut.ErrorMessage);
    }

    [Fact]
    public async Task VerifyCodeCommand_MissingCode_ShowsInlineErrorWithoutCallingTheService()
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = string.Empty };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_MissingCode, sut.ErrorMessage);
        Assert.Equal(0, service.SignInWithOtpCallCount);
    }

    [Fact]
    public async Task VerifyCodeCommand_InvalidOrExpiredCode_ShowsInvalidCodeMessage()
    {
        // Desktop OTP Authentication Migration: real backend's INVALID_OTP (401) - deliberately
        // covers wrong code, expired code, and no active code at all as one indistinguishable case.
        var service = new StubAuthenticationService { SignInWithOtpExceptionToThrow = new ApiAuthenticationException("401", statusCode: 401) };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = "000000" };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_InvalidCode, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task VerifyCodeCommand_InactiveUser_ShowsNotAuthorizedMessage()
    {
        // Desktop OTP Authentication Migration: real backend's INACTIVE_USER (403).
        var service = new StubAuthenticationService { SignInWithOtpExceptionToThrow = new ApiAuthenticationException("403", statusCode: 403) };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = "123456" };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_NotAuthorized, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task VerifyCodeCommand_RateLimited_ShowsRateLimitedMessage()
    {
        // Desktop OTP Authentication Migration: real backend's OTP_VERIFY_RATE_LIMITED (429),
        // distinct from OTP_REQUEST_RATE_LIMITED since it's per-phone verify-attempt limiting.
        var service = new StubAuthenticationService { SignInWithOtpExceptionToThrow = new ApiRateLimitException("429") };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = "123456" };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_RateLimited, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task VerifyCodeCommand_Success_RaisesSignedInAndLeavesNoErrorMessage()
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = "123456" };
        var raised = false;
        sut.SignedIn += (_, _) => raised = true;

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.True(raised);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, service.SignInWithOtpCallCount);
        Assert.Equal("+989123456789", service.LastPhoneNumber);
        Assert.Equal("123456", service.LastCode);
    }

    [Fact]
    public async Task VerifyCodeCommand_LocalFormattedPhoneNumber_NormalizesToE164BeforeCallingTheService()
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "09123456789", Code = "123456" };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, service.SignInWithOtpCallCount);
        Assert.Equal("+989123456789", service.LastPhoneNumber);
    }

    [Fact]
    public async Task ChangeNumberCommand_ResetsBackToThePhoneEntryStep()
    {
        var service = new StubAuthenticationService { ChallengeToReturn = new OtpChallenge("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60)) };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789" };
        await ExecuteAsync(sut.RequestCodeCommand, sut);
        sut.Code = "123456";

        sut.ChangeNumberCommand.Execute(null);

        Assert.False(sut.IsCodeSent);
        Assert.Equal(string.Empty, sut.Code);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void PhoneNumber_Setter_ClearsAnyExistingErrorMessage()
    {
        var sut = new MobileOtpLoginViewModel(new StubAuthenticationService(), new StubDelayScheduler());
        sut.RequestCodeCommand.Execute(null); // synchronously sets the "missing phone" error since PhoneNumber is empty

        sut.PhoneNumber = "+989123456789";

        Assert.Null(sut.ErrorMessage);
    }

    private static async Task ExecuteAsync(System.Windows.Input.ICommand command, MobileOtpLoginViewModel viewModel)
    {
        // AsyncRelayCommand.Execute is "async void" (ICommand's contract) - awaiting the
        // underlying task directly is not possible from here, so drive it through the
        // command and pump once. IsBusy going back to false is this test's own signal
        // that the fire-and-forget Execute has actually finished - same technique
        // LoginViewModelTests uses for LoginViewModel.SignInCommand.
        command.Execute(null);
        for (var i = 0; i < 100 && viewModel.IsBusy; i++)
        {
            await Task.Delay(10);
        }
    }
}
