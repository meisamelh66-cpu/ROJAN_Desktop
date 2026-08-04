using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>
/// Shared <see cref="IAuthenticationService"/> test double for
/// <see cref="MobileOtpLoginViewModelTests"/> and
/// <see cref="LoginWindowViewModelTests"/> - configurable success/failure
/// for both the credentials path and the OTP path, since
/// <see cref="LoginWindowViewModelTests"/> needs to drive both of its
/// composed child ViewModels through the same fake.
/// </summary>
internal sealed class StubAuthenticationService : IAuthenticationService
{
    public int SignInWithCredentialsCallCount { get; private set; }

    public int RequestOtpCallCount { get; private set; }

    public int SignInWithOtpCallCount { get; private set; }

    public string? LastEmail { get; private set; }

    public string? LastPhoneNumber { get; private set; }

    public string? LastCode { get; private set; }

    public Exception? SignInWithCredentialsExceptionToThrow { get; set; }

    public Exception? RequestOtpExceptionToThrow { get; set; }

    public Exception? SignInWithOtpExceptionToThrow { get; set; }

    public OtpChallenge ChallengeToReturn { get; set; } = new("+989123456789", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(60));

    public AuthenticationState CurrentState => AuthenticationState.SignedOut;

    public SessionIdentity? CurrentSession => null;

#pragma warning disable CS0067 // Required by IAuthenticationService; never subscribed to by the ViewModels under test.
    public event EventHandler<AuthenticationState>? StateChanged;
#pragma warning restore CS0067

    public Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        SignInWithCredentialsCallCount++;
        LastEmail = email;
        if (SignInWithCredentialsExceptionToThrow is not null)
        {
            throw SignInWithCredentialsExceptionToThrow;
        }

        return Task.CompletedTask;
    }

    public Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        RequestOtpCallCount++;
        LastPhoneNumber = phoneNumber;
        if (RequestOtpExceptionToThrow is not null)
        {
            throw RequestOtpExceptionToThrow;
        }

        return Task.FromResult(ChallengeToReturn);
    }

    public Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default)
    {
        SignInWithOtpCallCount++;
        LastPhoneNumber = phoneNumber;
        LastCode = code;
        if (SignInWithOtpExceptionToThrow is not null)
        {
            throw SignInWithOtpExceptionToThrow;
        }

        return Task.CompletedTask;
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
