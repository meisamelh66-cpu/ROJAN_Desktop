using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Owner App Backend Integration: the real, backend-connected
/// <see cref="IAuthenticationService"/> - replaces <see cref="LocalAuthenticationService"/>
/// in <c>ServiceCollectionExtensions.AddInfrastructure</c> (which stays in
/// the codebase, unreferenced, matching this repo's own Sprint 6
/// convention). <see cref="SignInWithCredentialsAsync"/> is the only real
/// entry point (see this method's own doc comment for why
/// <see cref="SignInAsync"/> throws here); it authenticates via
/// <see cref="AuthBootstrapHttpClient"/> (never <c>IApiClient</c> - see
/// that class's own doc comment) and hands the resulting token pair to
/// <see cref="ISessionService.CreateSessionFromTokensAsync"/>.
/// </summary>
public sealed class BackendAuthenticationService : IAuthenticationService, IDisposable
{
    private readonly AuthBootstrapHttpClient _authClient;
    private readonly ISessionService _sessionService;
    private readonly IDeviceRegistrationService _deviceRegistrationService;

    public BackendAuthenticationService(AuthBootstrapHttpClient authClient, ISessionService sessionService, IDeviceRegistrationService deviceRegistrationService)
    {
        _authClient = authClient;
        _sessionService = sessionService;
        _deviceRegistrationService = deviceRegistrationService;
        _sessionService.StateChanged += OnSessionStateChanged;
    }

    public AuthenticationState CurrentState => _sessionService.CurrentState;

    public SessionIdentity? CurrentSession => _sessionService.CurrentSession;

    public event EventHandler<AuthenticationState>? StateChanged;

    /// <summary>A backend-connected authentication service has no way to authenticate an already-resolved <see cref="UserIdentity"/> without credentials - use <see cref="SignInWithCredentialsAsync"/> instead.</summary>
    public Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(BackendAuthenticationService)} requires credentials - use {nameof(SignInWithCredentialsAsync)}.");

    public async Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? await _deviceRegistrationService.EnsureRegisteredAsync(cancellationToken).ConfigureAwait(false);

        var response = await _authClient
            .PostAsync<LoginRequest, AuthResponse>("/api/v1/auth/login", new LoginRequest(email, password), cancellationToken)
            .ConfigureAwait(false);

        var user = new UserIdentity(response.User.Id, response.User.FullName, response.User.Email);
        var now = DateTimeOffset.UtcNow;
        var accessToken = new AuthToken(response.AccessToken, now, response.AccessTokenExpiresAt);
        var refreshToken = new RefreshToken(response.RefreshToken, now, response.RefreshTokenExpiresAt);

        await _sessionService.CreateSessionFromTokensAsync(user, device, accessToken, refreshToken, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var response = await _authClient
            .PostAsync<OtpRequestRequest, OtpIssuedResponse>("/api/v1/auth/otp/request", new OtpRequestRequest(phoneNumber), cancellationToken)
            .ConfigureAwait(false);

        return new OtpChallenge(response.PhoneNumber, TimeSpan.FromSeconds(response.ExpiresInSeconds), TimeSpan.FromSeconds(response.CanResendAfterSeconds));
    }

    public async Task<OtpChallenge> ResendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var response = await _authClient
            .PostAsync<OtpRequestRequest, OtpIssuedResponse>("/api/v1/auth/otp/resend", new OtpRequestRequest(phoneNumber), cancellationToken)
            .ConfigureAwait(false);

        return new OtpChallenge(response.PhoneNumber, TimeSpan.FromSeconds(response.ExpiresInSeconds), TimeSpan.FromSeconds(response.CanResendAfterSeconds));
    }

    public async Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? await _deviceRegistrationService.EnsureRegisteredAsync(cancellationToken).ConfigureAwait(false);

        var response = await _authClient
            .PostAsync<OtpVerifyRequest, AuthResponse>("/api/v1/auth/otp/verify", new OtpVerifyRequest(phoneNumber, code, fullName), cancellationToken)
            .ConfigureAwait(false);

        var user = new UserIdentity(response.User.Id, response.User.FullName, response.User.Email, response.User.PhoneNumber);
        var now = DateTimeOffset.UtcNow;
        var accessToken = new AuthToken(response.AccessToken, now, response.AccessTokenExpiresAt);
        var refreshToken = new RefreshToken(response.RefreshToken, now, response.RefreshTokenExpiresAt);

        await _sessionService.CreateSessionFromTokensAsync(user, device, accessToken, refreshToken, cancellationToken).ConfigureAwait(false);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default) =>
        _sessionService.ExpireAsync(cancellationToken);

    public void Dispose() => _sessionService.StateChanged -= OnSessionStateChanged;

    private void OnSessionStateChanged(object? sender, AuthenticationState state) => StateChanged?.Invoke(this, state);
}
