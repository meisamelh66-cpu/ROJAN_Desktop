using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Exercises <see cref="LoginViewModel"/> - the error-handling matrix Task 3 (Invalid credentials / Network errors / missing input) explicitly requires, plus the success path's <see cref="LoginViewModel.SignedIn"/> signal.</summary>
public sealed class LoginViewModelTests
{
    [Fact]
    public async Task SignInCommand_MissingEmailOrPassword_ShowsInlineErrorWithoutCallingTheService()
    {
        var service = new StubAuthenticationService();
        var sut = new LoginViewModel(service) { Email = string.Empty, Password = string.Empty };

        await ExecuteAsync(sut);

        Assert.Equal(Strings.Login_Error_MissingCredentials, sut.ErrorMessage);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task SignInCommand_InvalidCredentials_ShowsInvalidCredentialsMessage()
    {
        var service = new StubAuthenticationService { ExceptionToThrow = new ApiAuthenticationException("401") };
        var sut = new LoginViewModel(service) { Email = "owner@example.com", Password = "wrong" };

        await ExecuteAsync(sut);

        Assert.Equal(Strings.Login_Error_InvalidCredentials, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Theory]
    [InlineData(typeof(ApiConnectivityException))]
    [InlineData(typeof(ApiTimeoutException))]
    public async Task SignInCommand_NetworkFailure_ShowsNetworkErrorMessage(Type exceptionType)
    {
        var exception = (ApiException)Activator.CreateInstance(exceptionType, "network down")!;
        var service = new StubAuthenticationService { ExceptionToThrow = exception };
        var sut = new LoginViewModel(service) { Email = "owner@example.com", Password = "supersecret123" };

        await ExecuteAsync(sut);

        Assert.Equal(Strings.Login_Error_Network, sut.ErrorMessage);
    }

    [Fact]
    public async Task SignInCommand_UnexpectedApiException_ShowsGenericErrorMessage()
    {
        var service = new StubAuthenticationService { ExceptionToThrow = new ApiException("boom") };
        var sut = new LoginViewModel(service) { Email = "owner@example.com", Password = "supersecret123" };

        await ExecuteAsync(sut);

        Assert.Equal(Strings.Login_Error_Generic, sut.ErrorMessage);
    }

    [Fact]
    public async Task SignInCommand_Success_RaisesSignedInAndLeavesNoErrorMessage()
    {
        var service = new StubAuthenticationService();
        var sut = new LoginViewModel(service) { Email = "owner@example.com", Password = "supersecret123" };
        var raised = false;
        sut.SignedIn += (_, _) => raised = true;

        await ExecuteAsync(sut);

        Assert.True(raised);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, service.CallCount);
        Assert.Equal("owner@example.com", service.LastEmail);
    }

    [Fact]
    public void Email_Setter_ClearsAnyExistingErrorMessage()
    {
        var sut = new LoginViewModel(new StubAuthenticationService());
        SetErrorMessageForTest(sut);

        sut.Email = "new@example.com";

        Assert.Null(sut.ErrorMessage);
    }

    private static async Task ExecuteAsync(LoginViewModel viewModel)
    {
        // AsyncRelayCommand.Execute is "async void" (ICommand's contract) - awaiting the
        // underlying task directly is not possible from here, so drive it through the
        // command and pump once. IsBusy going back to false is this test's own signal
        // that the fire-and-forget Execute has actually finished.
        viewModel.SignInCommand.Execute(null);
        for (var i = 0; i < 100 && viewModel.IsBusy; i++)
        {
            await Task.Delay(10);
        }
    }

    /// <summary>Drives the private ErrorMessage setter indirectly via a failed sign-in, purely to set up the "clears on edit" test above without needing reflection.</summary>
    private static void SetErrorMessageForTest(LoginViewModel viewModel)
    {
        viewModel.Password = "x";
        viewModel.Email = string.Empty;
        viewModel.SignInCommand.Execute(null);
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public int CallCount { get; private set; }

        public string? LastEmail { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public AuthenticationState CurrentState => AuthenticationState.SignedOut;

        public SessionIdentity? CurrentSession => null;

#pragma warning disable CS0067 // Required by IAuthenticationService; LoginViewModel never subscribes to it, only Shell does.
        public event EventHandler<AuthenticationState>? StateChanged;
#pragma warning restore CS0067

        public Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastEmail = email;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }

        public Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
