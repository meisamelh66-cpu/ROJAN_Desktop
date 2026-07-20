using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Domain.Identity;

namespace Rojan.Desktop.Infrastructure.Identity;

/// <summary>
/// Default <see cref="IDeviceRegistrationService"/>. Persists
/// <c>%LocalAppData%\RojanDesktop\identity\device.json</c> - same "one
/// concern, one file" shape <c>Shell.Organizations.CurrentSessionService</c>'s
/// own doc comment establishes, grouped under an <c>identity\</c>
/// subfolder since Phase 25 adds several such files. The device
/// <see cref="DeviceIdentity.Id"/> is minted once (a random, unguessable
/// value - never derived from hardware, so it cannot double as a tracking
/// fingerprint on its own) and never changes; <see cref="DeviceIdentity.Fingerprint"/>
/// is recomputed from <see cref="Environment.MachineName"/>/
/// <see cref="Environment.OSVersion"/>/<see cref="Environment.ProcessorCount"/>
/// on every call so hardware drift is observable without minting a new
/// device identity for it. The installation id is likewise minted once
/// per install (a fresh install, even on the same machine, gets a new
/// one) - see <see cref="InstallationIdentity"/>'s own doc comment.
/// </summary>
public sealed class DeviceRegistrationService : IDeviceRegistrationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public DeviceRegistrationService()
        : this(DefaultFilePath())
    {
    }

    internal DeviceRegistrationService(string filePath)
    {
        _filePath = filePath;
    }

    public DeviceIdentity? CurrentDevice { get; private set; }

    public InstallationIdentity? CurrentInstallation { get; private set; }

    public Task<DeviceIdentity> EnsureRegisteredAsync(CancellationToken cancellationToken = default)
    {
        var fingerprint = ComputeFingerprint();
        var now = DateTimeOffset.UtcNow;

        var persisted = ReadPersisted();
        if (persisted is not null)
        {
            // Fingerprint drift is recorded (not blocked) - this phase
            // only observes, a future phase decides what drift means for
            // re-registration/attestation.
            var device = persisted.Device with { Fingerprint = fingerprint };
            CurrentDevice = device;
            CurrentInstallation = persisted.Installation;
            if (fingerprint != persisted.Device.Fingerprint)
            {
                Persist(device, persisted.Installation);
            }

            return Task.FromResult(device);
        }

        var newDevice = new DeviceIdentity(
            Id: Guid.NewGuid().ToString("N"),
            Fingerprint: fingerprint,
            MachineName: Environment.MachineName,
            OperatingSystemDescription: Environment.OSVersion.VersionString,
            RegisteredAt: now);

        var newInstallation = new InstallationIdentity(
            Id: Guid.NewGuid().ToString("N"),
            AppVersion: typeof(DeviceRegistrationService).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            InstalledAt: now);

        CurrentDevice = newDevice;
        CurrentInstallation = newInstallation;
        Persist(newDevice, newInstallation);

        return Task.FromResult(newDevice);
    }

    /// <summary>Not a cryptographic device identifier (it must stay stable across ordinary driver/BIOS-reported-order changes) - a SHA-256 hash of coarse, stable machine characteristics, hex-encoded so it is safe to persist/transmit as plain text.</summary>
    private static string ComputeFingerprint()
    {
        var raw = $"{Environment.MachineName}|{Environment.OSVersion.VersionString}|{Environment.ProcessorCount}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private PersistedRegistration? ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PersistedRegistration>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private void Persist(DeviceIdentity device, InstallationIdentity installation)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new PersistedRegistration(device, installation), SerializerOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "identity", "device.json");

    private sealed record PersistedRegistration(DeviceIdentity Device, InstallationIdentity Installation);
}
