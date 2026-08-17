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
    [InlineData("not-a-phone-number")]
    [InlineData("091234567")] // too short - not a real 10-digit Iranian subscriber number
    public async Task RequestCodeCommand_InvalidPhoneNumberFormat_ShowsInlineErrorWithoutCallingTheService(string phoneNumber)
    {
        var service = new StubAuthenticationService();
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = phoneNumber };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_InvalidPhone, sut.ErrorMessage);
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
        // Covers rate-limiting (429) among other non-2xx responses - the client does not
        // parse ApiErrorResponse yet (see that contract's own doc comment), so every
        // rejected request surfaces the same generic message, same as the email flow.
        var service = new StubAuthenticationService { RequestOtpExceptionToThrow = new ApiException("Too many requests") };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789" };

        await ExecuteAsync(sut.RequestCodeCommand, sut);

        Assert.Equal(Strings.Login_Error_Generic, sut.ErrorMessage);
        Assert.False(sut.IsCodeSent);
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
        var service = new StubAuthenticationService { SignInWithOtpExceptionToThrow = new ApiAuthenticationException("401") };
        var sut = new MobileOtpLoginViewModel(service, new StubDelayScheduler()) { PhoneNumber = "+989123456789", Code = "000000" };

        await ExecuteAsync(sut.VerifyCodeCommand, sut);

        Assert.Equal(Strings.Login_Mobile_Error_InvalidCode, sut.ErrorMessage);
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
