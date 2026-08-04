using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Exercises <see cref="LoginWindowViewModel"/> - the composition of the primary Mobile Number + OTP flow and the secondary Email + Password flow behind one mode flag and one bubbled <see cref="LoginWindowViewModel.SignedIn"/> signal.</summary>
public sealed class LoginWindowViewModelTests
{
    [Fact]
    public void IsEmailModeActive_DefaultsToFalse_MobileIsThePrimaryFlow()
    {
        var sut = CreateViewModel(out _, out _);

        Assert.False(sut.IsEmailModeActive);
    }

    [Fact]
    public void SwitchToEmailLoginCommand_ActivatesTheEmailFlow()
    {
        var sut = CreateViewModel(out _, out _);

        sut.SwitchToEmailLoginCommand.Execute(null);

        Assert.True(sut.IsEmailModeActive);
    }

    [Fact]
    public void SwitchToMobileLoginCommand_ReturnsToTheMobileFlow()
    {
        var sut = CreateViewModel(out _, out _);
        sut.SwitchToEmailLoginCommand.Execute(null);

        sut.SwitchToMobileLoginCommand.Execute(null);

        Assert.False(sut.IsEmailModeActive);
    }

    [Fact]
    public async Task MobileLoginSignedIn_BubblesThroughTheComposedSignedInEvent()
    {
        var sut = CreateViewModel(out var mobileService, out _);
        var raised = false;
        sut.SignedIn += (_, _) => raised = true;

        sut.MobileLogin.PhoneNumber = "+989123456789";
        sut.MobileLogin.Code = "123456";
        sut.MobileLogin.VerifyCodeCommand.Execute(null);
        for (var i = 0; i < 100 && sut.MobileLogin.IsBusy; i++)
        {
            await Task.Delay(10);
        }

        Assert.True(raised);
        Assert.Equal(1, mobileService.SignInWithOtpCallCount);
    }

    [Fact]
    public async Task EmailLoginSignedIn_BubblesThroughTheComposedSignedInEvent()
    {
        var sut = CreateViewModel(out _, out var emailService);
        var raised = false;
        sut.SignedIn += (_, _) => raised = true;

        sut.EmailLogin.Email = "owner@example.com";
        sut.EmailLogin.Password = "supersecret123";
        sut.EmailLogin.SignInCommand.Execute(null);
        for (var i = 0; i < 100 && sut.EmailLogin.IsBusy; i++)
        {
            await Task.Delay(10);
        }

        Assert.True(raised);
        Assert.Equal(1, emailService.SignInWithCredentialsCallCount);
    }

    private static LoginWindowViewModel CreateViewModel(out StubAuthenticationService mobileService, out StubAuthenticationService emailService)
    {
        mobileService = new StubAuthenticationService();
        emailService = new StubAuthenticationService();
        var mobileLogin = new MobileOtpLoginViewModel(mobileService, new StubDelayScheduler());
        var emailLogin = new LoginViewModel(emailService);
        return new LoginWindowViewModel(mobileLogin, emailLogin);
    }
}
