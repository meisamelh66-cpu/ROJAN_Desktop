using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>Backed by a real <see cref="DeviceRegistrationService"/> over its own temp file - exercises the full issue-then-persist-then-validate path.</summary>
public sealed class LocalCertificateServiceTests : IDisposable
{
    private readonly string _certificateFilePath;
    private readonly string _deviceFilePath;

    public LocalCertificateServiceTests()
    {
        var root = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        _certificateFilePath = Path.Combine(root, "certificate.json");
        _deviceFilePath = Path.Combine(root, "device.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_certificateFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private LocalCertificateService CreateService() =>
        new(new DeviceRegistrationService(_deviceFilePath), _certificateFilePath);

    [Fact]
    public async Task InitializeAsync_NoPersistedCertificate_ReportsNotIssued()
    {
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(CertificateState.NotIssued, service.CurrentState);
        Assert.Null(service.CurrentCertificate);
    }

    [Fact]
    public async Task IssueAsync_IssuesAValidCertificateExpiringOneYearOut()
    {
        var service = CreateService();

        var certificate = await service.IssueAsync();

        Assert.Equal(CertificateState.Valid, service.CurrentState);
        Assert.True(certificate.ExpiresAt > DateTimeOffset.UtcNow.AddDays(364));
        Assert.True(certificate.ExpiresAt < DateTimeOffset.UtcNow.AddDays(366));
        Assert.True(File.Exists(_certificateFilePath));
    }

    [Fact]
    public async Task RenewAsync_NoCertificateYet_ThrowsInvalidOperationException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenewAsync());
    }

    [Fact]
    public async Task RenewAsync_ReplacesTheCertificateWithANewThumbprintAndLaterExpiry()
    {
        var service = CreateService();
        var original = await service.IssueAsync();

        var renewed = await service.RenewAsync();

        Assert.NotEqual(original.Thumbprint, renewed.Thumbprint);
        Assert.True(renewed.ExpiresAt > original.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ReDerivesStateFromCurrentCertificateAndNow()
    {
        var service = CreateService();
        await service.IssueAsync();

        var state = service.Validate();

        Assert.Equal(CertificateState.Valid, state);
    }
}
