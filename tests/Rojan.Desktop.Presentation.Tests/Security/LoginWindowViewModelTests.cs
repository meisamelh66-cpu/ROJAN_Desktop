using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Exercises <see cref="LoginWindowViewModel"/> - the thin wrapper around the Phone Number + OTP flow (Desktop OTP Authentication Migration), and its bubbled <see cref="LoginWindowViewModel.SignedIn"/> signal.</summary>
public sealed class LoginWindowViewModelTests
{
    [Fact]
    public async Task MobileLoginSignedIn_BubblesThroughTheComposedSignedInEvent()
    {
        var sut = CreateViewModel(out var mobileService);
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

    private static LoginWindowViewModel CreateViewModel(out StubAuthenticationService mobileService)
    {
        mobileService = new StubAuthenticationService();
        var mobileLogin = new MobileOtpLoginViewModel(mobileService, new StubDelayScheduler());
        return new LoginWindowViewModel(mobileLogin);
    }
}
