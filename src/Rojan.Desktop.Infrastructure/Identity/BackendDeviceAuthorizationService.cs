using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Identity;

namespace Rojan.Desktop.Infrastructure.Identity;

/// <summary>
/// Phase B: Windows Reception Device Registration. The real, backend-connected
/// <see cref="IDeviceAuthorizationService"/> - calls ROJAN_Backend's
/// <c>POST /api/v1/users/me/devices</c> (Phase A,
/// <c>ai.rojan.backend.api.device.DeviceController</c>) via the existing
/// <see cref="IApiClient"/> pipeline, never a second HTTP client (see that
/// interface's own doc comment - <see cref="Infrastructure.Api.AuthBootstrapHttpClient"/>
/// is deliberately NOT used here: unlike login/refresh, this call happens
/// after a real session already exists, so it needs the generic pipeline's
/// auth-header attachment and 401-refresh-and-retry-once behavior, not
/// AuthBootstrapHttpClient's bootstrap-only shape).
///
/// Never generates, regenerates, or reinterprets device/installation
/// identity - <see cref="IDeviceRegistrationService.CurrentDevice"/>/
/// <see cref="IDeviceRegistrationService.CurrentInstallation"/> are read
/// as-is; <c>Shell.App.OnStartup</c> already guarantees
/// <see cref="IDeviceRegistrationService.EnsureRegisteredAsync"/> has run
/// before this service is ever called (see that method's own ordering
/// comment), so both are expected to already be non-null - a null read
/// here is a genuine startup-ordering bug, not a recoverable runtime
/// condition, hence the guard throws rather than silently sending an
/// incomplete request.
///
/// Every non-success path returns a [DeviceRegistrationResult] instead of
/// letting a failure look like, or silently become, a success - see that
/// type's own doc comment for the exact outcome contract. Nothing here
/// implements a heartbeat scheduler/background retry - a caller (Shell) is
/// free to call <see cref="RegisterAsync"/> again on a later launch using
/// the exact same, unchanged device identity; that repeat call is exactly
/// what makes it a heartbeat, not a distinct mechanism.
/// </summary>
public sealed class BackendDeviceAuthorizationService : IDeviceAuthorizationService
{
    private const string DevicesPath = "/api/v1/users/me/devices";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IApiClient _apiClient;
    private readonly IDeviceRegistrationService _deviceRegistrationService;
    private readonly string _localStateFilePath;

    public BackendDeviceAuthorizationService(IApiClient apiClient, IDeviceRegistrationService deviceRegistrationService)
        : this(apiClient, deviceRegistrationService, DefaultLocalStateFilePath())
    {
    }

    /// <summary>Test-only seam (see <c>Rojan.Desktop.Infrastructure.csproj</c>'s <c>InternalsVisibleTo</c>) - mirrors <see cref="DeviceRegistrationService"/>'s own internal-constructor pattern, so tests never touch the real <c>%LocalAppData%</c>.</summary>
    internal BackendDeviceAuthorizationService(IApiClient apiClient, IDeviceRegistrationService deviceRegistrationService, string localStateFilePath)
    {
        _apiClient = apiClient;
        _deviceRegistrationService = deviceRegistrationService;
        _localStateFilePath = localStateFilePath;
    }

    public async Task<DeviceRegistrationResult> RegisterAsync(string salonId, CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? throw new InvalidOperationException($"{nameof(IDeviceRegistrationService.EnsureRegisteredAsync)} must complete before device registration can run.");
        var installation = _deviceRegistrationService.CurrentInstallation
            ?? throw new InvalidOperationException($"{nameof(IDeviceRegistrationService.EnsureRegisteredAsync)} must complete before device registration can run.");

        var request = new DeviceRegistrationRequest(salonId, device.Id, device.Fingerprint, installation.Id);

        ApiResponse<DeviceRegistrationResponse> response;
        try
        {
            response = await _apiClient
                .PostAsync<DeviceRegistrationRequest, DeviceRegistrationResponse>(DevicesPath, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApiAuthenticationException exception)
        {
            // 401/403 - the caller does not actually control salonId, or the session itself is
            // no longer valid. Never persisted, never reported as success.
            return DeviceRegistrationResult.AuthorizationDenied(exception.Message);
        }
        catch (ApiConnectivityException exception)
        {
            // Transient - offline, or the transport itself failed. Safe to retry later with the
            // exact same device identity; the session and any prior local state are untouched.
            return DeviceRegistrationResult.NetworkUnavailable(exception.Message);
        }
        catch (ApiTimeoutException exception)
        {
            return DeviceRegistrationResult.NetworkUnavailable(exception.Message);
        }
        catch (ApiException exception)
        {
            // Any other pipeline-level failure this client doesn't have a more specific case for -
            // still reported as a real rejection, never silently treated as success.
            return DeviceRegistrationResult.Rejected(exception.Message);
        }

        if (!response.IsSuccess || response.Data is null)
        {
            return DeviceRegistrationResult.Rejected(response.ErrorMessage);
        }

        PersistLocalState(response.Data);
        return DeviceRegistrationResult.Registered(response.Data);
    }

    /// <summary>
    /// Persists only non-secret, informational state (the backend-assigned record id, salon,
    /// deviceId, and timestamps) - never a token, never anything that itself grants access. This
    /// file is never read to skip or shortcut a future real backend call: <see cref="RegisterAsync"/>
    /// always re-registers against the backend on every call, so this local record can never
    /// override or stand in for real backend authorization - it exists purely so a future caller
    /// (diagnostics, a future UI) has something to read without re-calling the backend for display
    /// purposes only.
    /// </summary>
    private void PersistLocalState(DeviceRegistrationResponse registration)
    {
        var directory = Path.GetDirectoryName(_localStateFilePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(registration, SerializerOptions);
        File.WriteAllText(_localStateFilePath, json);
    }

    private static string DefaultLocalStateFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "identity", "device-registration.json");
}
