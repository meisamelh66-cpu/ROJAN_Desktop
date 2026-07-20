using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="ICertificateService"/>. Issues a locally-generated
/// <see cref="OfflineCertificate"/> (see that type's own doc comment for
/// why this is not a real X.509/PKI certificate yet) bound to the current
/// device's registration, persisted to
/// <c>%LocalAppData%\RojanDesktop\security\certificate.json</c>. Validity
/// window is 365 days from issue/renewal - <see cref="CertificateRules"/>
/// (Domain, pure) does the actual state derivation, this class only owns
/// I/O and issuance.
/// </summary>
public sealed class LocalCertificateService : ICertificateService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly TimeSpan ValidityPeriod = TimeSpan.FromDays(365);

    private readonly IDeviceRegistrationService _deviceRegistrationService;
    private readonly string _filePath;

    public LocalCertificateService(IDeviceRegistrationService deviceRegistrationService)
        : this(deviceRegistrationService, DefaultFilePath())
    {
    }

    internal LocalCertificateService(IDeviceRegistrationService deviceRegistrationService, string filePath)
    {
        _deviceRegistrationService = deviceRegistrationService;
        _filePath = filePath;
    }

    public OfflineCertificate? CurrentCertificate { get; private set; }

    public CertificateState CurrentState { get; private set; } = CertificateState.NotIssued;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        CurrentCertificate = ReadPersisted();
        Validate();
        return Task.CompletedTask;
    }

    public async Task<OfflineCertificate> IssueAsync(CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? await _deviceRegistrationService.EnsureRegisteredAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var thumbprint = ComputeThumbprint(device.Id, now);
        var certificate = new OfflineCertificate(device.Id, thumbprint, now, now + ValidityPeriod);

        CurrentCertificate = certificate;
        Persist(certificate);
        Validate();
        return certificate;
    }

    public Task<OfflineCertificate> RenewAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentCertificate is null)
        {
            throw new InvalidOperationException("No certificate has been issued yet - call IssueAsync first.");
        }

        return IssueAsync(cancellationToken);
    }

    public CertificateState Validate()
    {
        CurrentState = CertificateRules.DetermineState(CurrentCertificate, DateTimeOffset.UtcNow);
        return CurrentState;
    }

    private static string ComputeThumbprint(string subjectId, DateTimeOffset issuedAt)
    {
        var raw = $"{subjectId}|{issuedAt:O}|{Guid.NewGuid():N}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private OfflineCertificate? ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<OfflineCertificate>(json, SerializerOptions);
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

    private void Persist(OfflineCertificate certificate)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(certificate, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "security", "certificate.json");
}
