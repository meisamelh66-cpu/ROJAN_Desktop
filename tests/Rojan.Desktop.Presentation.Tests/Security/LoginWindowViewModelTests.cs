using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Exercises <see cref="LoginWindowViewModel"/> - the thin wrapper around the Email/Password flow, and its bubbled <see cref="LoginWindowViewModel.SignedIn"/> signal.</summary>
public sealed class LoginWindowViewModelTests
{
    [Fact]
    public async Task EmailLoginSignedIn_BubblesThroughTheComposedSignedInEvent()
    {
        var sut = CreateViewModel(out var emailService);
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

    private static LoginWindowViewModel CreateViewModel(out StubAuthenticationService emailService)
    {
        emailService = new StubAuthenticationService();
        var emailLogin = new LoginViewModel(emailService);
        return new LoginWindowViewModel(emailLogin);
    }
}
