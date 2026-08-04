using System.Net;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>
/// Backed by a real <see cref="BackendSessionService"/> (in-memory secure
/// storage) and a real <see cref="DeviceRegistrationService"/> (temp file) -
/// same "exercise the full workflow, not mocked collaborators" philosophy
/// as <see cref="LocalAuthenticationServiceTests"/>. Only the network edge
/// (<see cref="AuthBootstrapHttpClient"/>) is faked.
/// </summary>
public sealed class BackendAuthenticationServiceTests : IDisposable
{
    private static readonly Uri TestBaseAddress = new("https://api.rojan.test/");

    private readonly string _root = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SignInWithCredentialsAsync_ValidCredentials_EstablishesAnAuthenticatedSessionFromTheRealBackendTokens()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/api/v1/auth/login", request.RequestUri?.AbsolutePath);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
                {
                  "user": {"id":"owner-1","email":"owner@example.com","fullName":"Salon Owner","role":"MANAGER"},
                  "accessToken":"real-access","accessTokenExpiresAt":"2026-08-04T12:15:00Z",
                  "refreshToken":"real-refresh","refreshTokenExpiresAt":"2026-09-03T12:00:00Z"
                }
                """));
        });
        using var service = CreateService(handler);

        await service.SignInWithCredentialsAsync("owner@example.com", "supersecret123");

        Assert.Equal("owner-1", service.CurrentSession?.UserId);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
        Assert.NotNull(service.CurrentSession);
    }

    [Fact]
    public async Task SignInWithCredentialsAsync_WrongPassword_ThrowsApiAuthenticationException()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => service.SignInWithCredentialsAsync("owner@example.com", "wrong-password"));
        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
    }

    [Fact]
    public async Task RequestOtpAsync_ValidPhoneNumber_ReturnsTheIssuedChallengeWithoutEstablishingASession()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/api/v1/auth/otp/request", request.RequestUri?.AbsolutePath);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
                {"phoneNumber":"+989123456789","expiresInSeconds":120,"canResendAfterSeconds":60}
                """));
        });
        using var service = CreateService(handler);

        var challenge = await service.RequestOtpAsync("+989123456789");

        Assert.Equal("+989123456789", challenge.PhoneNumber);
        Assert.Equal(TimeSpan.FromSeconds(120), challenge.ExpiresIn);
        Assert.Equal(TimeSpan.FromSeconds(60), challenge.CanResendAfter);
        Assert.Null(service.CurrentSession);
        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
    }

    [Fact]
    public async Task RequestOtpAsync_RateLimited_ThrowsApiException()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse((HttpStatusCode)429, """{"errorCode":"OTP_REQUEST_RATE_LIMITED","message":"Too many OTP requests"}""")));
        using var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiException>(() => service.RequestOtpAsync("+989123456789"));
    }

    [Fact]
    public async Task SignInWithOtpAsync_ValidCode_EstablishesAnAuthenticatedSessionFromTheRealBackendTokens()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/api/v1/auth/otp/verify", request.RequestUri?.AbsolutePath);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
                {
                  "user": {"id":"owner-1","email":null,"phoneNumber":"+989123456789","fullName":"Salon Owner","role":"MANAGER"},
                  "accessToken":"real-access","accessTokenExpiresAt":"2026-08-04T12:15:00Z",
                  "refreshToken":"real-refresh","refreshTokenExpiresAt":"2026-09-03T12:00:00Z"
                }
                """));
        });
        using var service = CreateService(handler);

        await service.SignInWithOtpAsync("+989123456789", "482913");

        Assert.Equal("owner-1", service.CurrentSession?.UserId);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
    }

    [Fact]
    public async Task SignInWithOtpAsync_InvalidOrExpiredCode_ThrowsApiAuthenticationException()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.Unauthorized, """{"errorCode":"INVALID_OTP","message":"Invalid or expired OTP"}""")));
        using var service = CreateService(handler);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => service.SignInWithOtpAsync("+989123456789", "000000"));
        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
    }

    [Fact]
    public async Task SignInAsync_WithAnAlreadyResolvedIdentity_ThrowsNotSupported()
    {
        using var service = CreateService(new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("Should not be called.")));

        await Assert.ThrowsAsync<NotSupportedException>(() => service.SignInAsync(new Rojan.Desktop.Domain.Identity.UserIdentity("u", "n", null)));
    }

    [Fact]
    public async Task SignOutAsync_ClearsTheSession()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, """
            {"user":{"id":"owner-1","email":"o@example.com","fullName":"Owner","role":"MANAGER"},"accessToken":"a","accessTokenExpiresAt":"2026-08-04T12:15:00Z","refreshToken":"r","refreshTokenExpiresAt":"2026-09-03T12:00:00Z"}
            """)));
        using var service = CreateService(handler);
        await service.SignInWithCredentialsAsync("o@example.com", "pw");

        await service.SignOutAsync();

        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.Null(service.CurrentSession);
    }

    private BackendAuthenticationService CreateService(HttpMessageHandler handler)
    {
        var authClient = new AuthBootstrapHttpClient(handler, TestBaseAddress);
        var sessionService = new BackendSessionService(authClient, new BackendSessionServiceTests.StubSecureStorageService());
        var deviceRegistration = new DeviceRegistrationService(Path.Combine(_root, "device.json"));
        return new BackendAuthenticationService(authClient, sessionService, deviceRegistration);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }
}
