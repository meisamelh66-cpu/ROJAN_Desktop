using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Infrastructure.Identity;

namespace Rojan.Desktop.Infrastructure.Tests.Identity;

/// <summary>
/// Phase B: Windows Reception Device Registration. Exercises
/// <see cref="BackendDeviceAuthorizationService"/> - only the HTTP transport
/// (<see cref="IApiClient"/>) and the local device identity
/// (<see cref="IDeviceRegistrationService"/>) are faked, same "exercise the
/// real workflow" convention every other <c>Backend*ServiceTests</c>/
/// <c>Backend*RepositoryTests</c> class in this project already uses.
/// </summary>
public sealed class BackendDeviceAuthorizationServiceTests
{
    private const string DevicesPath = "/api/v1/users/me/devices";
    private const string SalonId = "salon-1";

    private static readonly DateTimeOffset RegisteredAt = DateTimeOffset.UtcNow;

    [Fact]
    public async Task RegisterAsync_Success_ReturnsRegisteredWithBackendResponse()
    {
        var apiClient = new StubApiClient();
        apiClient.PostResponse = new DeviceRegistrationResponse("device-record-1", SalonId, "device-1", RegisteredAt, RegisteredAt, null);
        var localStateFilePath = TempFilePath();
        var service = CreateService(apiClient, localStateFilePath: localStateFilePath);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.Registered, result.Outcome);
        Assert.NotNull(result.Registration);
        Assert.Equal("device-record-1", result.Registration!.Id);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_Success_SendsSalonId_DeviceId_Fingerprint_And_InstallationId_FromDeviceRegistrationService()
    {
        var apiClient = new StubApiClient();
        apiClient.PostResponse = new DeviceRegistrationResponse("device-record-1", SalonId, "device-1", RegisteredAt, RegisteredAt, null);
        var deviceRegistration = new StubDeviceRegistrationService(
            new DeviceIdentity("device-1", "fingerprint-1", "machine", "os", RegisteredAt),
            new InstallationIdentity("installation-1", "1.0.0", RegisteredAt));
        var service = CreateService(apiClient, deviceRegistration);

        await service.RegisterAsync(SalonId);

        Assert.NotNull(apiClient.LastPostCall);
        Assert.Equal(DevicesPath, apiClient.LastPostCall!.Value.Path);
        var request = Assert.IsType<DeviceRegistrationRequest>(apiClient.LastPostCall.Value.Body);
        Assert.Equal(SalonId, request.SalonId);
        Assert.Equal("device-1", request.DeviceId);
        Assert.Equal("fingerprint-1", request.Fingerprint);
        Assert.Equal("installation-1", request.InstallationId);
    }

    [Fact]
    public async Task RegisterAsync_NeverCallsEnsureRegisteredAsync_DeviceIdentityIsNeverRegenerated()
    {
        // StubDeviceRegistrationService.EnsureRegisteredAsync throws if called at all - this test
        // passing (rather than throwing) is itself the proof RegisterAsync only ever reads
        // CurrentDevice/CurrentInstallation, never mints or refreshes device identity.
        var apiClient = new StubApiClient { PostResponse = new DeviceRegistrationResponse("device-record-1", SalonId, "device-1", RegisteredAt, RegisteredAt, null) };
        var service = CreateService(apiClient);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.Registered, result.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_Backend403_ReturnsAuthorizationDenied_NotSwallowed()
    {
        var apiClient = new StubApiClient { PostException = new ApiAuthenticationException("Request was rejected with status 403.", 403) };
        var service = CreateService(apiClient);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.AuthorizationDenied, result.Outcome);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Registration);
    }

    [Fact]
    public async Task RegisterAsync_Backend401AfterRefreshStillFails_ReturnsAuthorizationDenied_ViaExistingAuthExceptionType()
    {
        // IApiClient itself already attempts one refresh-and-retry for a 401 (HttpApiClient.EnsureAuthenticatedAsync)
        // before ever surfacing ApiAuthenticationException - this simulates that already-handled outcome, the
        // one shape a 401 can reach this service as.
        var apiClient = new StubApiClient { PostException = new ApiAuthenticationException("Request was still rejected with status 401 after refreshing the session.", 401) };
        var service = CreateService(apiClient);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.AuthorizationDenied, result.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_ValidationRejection_ReturnsRejected_WithSafeErrorMessage()
    {
        var apiClient = new StubApiClient { PostFailure = (400, "deviceId must not be blank") };
        var service = CreateService(apiClient);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.Rejected, result.Outcome);
        Assert.Equal("deviceId must not be blank", result.ErrorMessage);
        Assert.Null(result.Registration);
    }

    [Fact]
    public async Task RegisterAsync_ConnectivityFailure_ReturnsNetworkUnavailable_NeverFakeSuccess()
    {
        var apiClient = new StubApiClient { PostException = new ApiConnectivityException("No network connection is available.") };
        var localStateFilePath = TempFilePath();
        var service = CreateService(apiClient, localStateFilePath: localStateFilePath);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.NetworkUnavailable, result.Outcome);
        Assert.Null(result.Registration);
        Assert.False(File.Exists(localStateFilePath), "a network failure must never persist local registration state");
    }

    [Fact]
    public async Task RegisterAsync_TimeoutFailure_ReturnsNetworkUnavailable()
    {
        var apiClient = new StubApiClient { PostException = new ApiTimeoutException("Request timed out.") };
        var service = CreateService(apiClient);

        var result = await service.RegisterAsync(SalonId);

        Assert.Equal(DeviceRegistrationOutcome.NetworkUnavailable, result.Outcome);
    }

    [Fact]
    public async Task RegisterAsync_OnlyPersistsLocalStateOnSuccess()
    {
        var apiClient = new StubApiClient { PostResponse = new DeviceRegistrationResponse("device-record-1", SalonId, "device-1", RegisteredAt, RegisteredAt, null) };
        var localStateFilePath = TempFilePath();
        var service = CreateService(apiClient, localStateFilePath: localStateFilePath);

        await service.RegisterAsync(SalonId);

        Assert.True(File.Exists(localStateFilePath));
        var json = await File.ReadAllTextAsync(localStateFilePath);
        Assert.DoesNotContain("fingerprint-1", json, StringComparison.Ordinal); // no secret/identity input ever persisted, only the backend's own response
    }

    [Fact]
    public async Task RegisterAsync_RetryAfterTransientFailure_UsesTheExactSameDeviceIdentity()
    {
        var deviceRegistration = new StubDeviceRegistrationService(
            new DeviceIdentity("device-1", "fingerprint-1", "machine", "os", RegisteredAt),
            new InstallationIdentity("installation-1", "1.0.0", RegisteredAt));
        var apiClient = new StubApiClient { PostException = new ApiConnectivityException("offline") };
        var service = CreateService(apiClient, deviceRegistration);

        var firstAttempt = await service.RegisterAsync(SalonId);
        Assert.Equal(DeviceRegistrationOutcome.NetworkUnavailable, firstAttempt.Outcome);
        var firstRequest = Assert.IsType<DeviceRegistrationRequest>(apiClient.LastPostCall!.Value.Body);

        apiClient.PostException = null;
        apiClient.PostResponse = new DeviceRegistrationResponse("device-record-1", SalonId, "device-1", RegisteredAt, RegisteredAt, null);
        var secondAttempt = await service.RegisterAsync(SalonId);
        Assert.Equal(DeviceRegistrationOutcome.Registered, secondAttempt.Outcome);
        var secondRequest = Assert.IsType<DeviceRegistrationRequest>(apiClient.LastPostCall.Value.Body);

        Assert.Equal(firstRequest.DeviceId, secondRequest.DeviceId);
        Assert.Equal(firstRequest.Fingerprint, secondRequest.Fingerprint);
        Assert.Equal(firstRequest.InstallationId, secondRequest.InstallationId);
    }

    private static string TempFilePath() => Path.Combine(Path.GetTempPath(), $"rojan-device-registration-test-{Guid.NewGuid():N}.json");

    private static BackendDeviceAuthorizationService CreateService(
        StubApiClient apiClient,
        StubDeviceRegistrationService? deviceRegistration = null,
        string? localStateFilePath = null) =>
        new(apiClient, deviceRegistration ?? new StubDeviceRegistrationService(), localStateFilePath ?? TempFilePath());

    private sealed class StubDeviceRegistrationService : IDeviceRegistrationService
    {
        public StubDeviceRegistrationService()
            : this(
                new DeviceIdentity("device-1", "fingerprint-1", "machine", "os", RegisteredAt),
                new InstallationIdentity("installation-1", "1.0.0", RegisteredAt))
        {
        }

        public StubDeviceRegistrationService(DeviceIdentity device, InstallationIdentity installation)
        {
            CurrentDevice = device;
            CurrentInstallation = installation;
        }

        public DeviceIdentity? CurrentDevice { get; }

        public InstallationIdentity? CurrentInstallation { get; }

        public Task<DeviceIdentity> EnsureRegisteredAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService must never (re)generate device identity - CurrentDevice/CurrentInstallation are already set.");
    }

    private sealed class StubApiClient : IApiClient
    {
        public object? PostResponse { get; set; }

        public (int? Status, string Message)? PostFailure { get; set; }

        public ApiException? PostException { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPostCall = (path, body);

            if (PostException is not null)
            {
                throw PostException;
            }

            if (PostFailure is { } failure)
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            return Task.FromResult(ApiResponseFactory.Success((TResponse)PostResponse!, 200));
        }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never gets.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDeviceAuthorizationService never gets bytes.");
    }
}
