using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Domain.Tests.Security;

public sealed class CertificateRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DetermineState_NullCertificate_ReturnsNotIssued()
    {
        var result = CertificateRules.DetermineState(null, Now);

        Assert.Equal(CertificateState.NotIssued, result);
    }

    [Fact]
    public void DetermineState_FarFromExpiry_ReturnsValid()
    {
        var certificate = new OfflineCertificate("subject-1", "thumb-1", Now.AddDays(-30), Now.AddDays(300));

        var result = CertificateRules.DetermineState(certificate, Now);

        Assert.Equal(CertificateState.Valid, result);
    }

    [Fact]
    public void DetermineState_WithinExpiringSoonWindow_ReturnsExpiringSoon()
    {
        var certificate = new OfflineCertificate("subject-1", "thumb-1", Now.AddDays(-335), Now.Add(CertificateRules.ExpiringSoonWindow).AddDays(-1));

        var result = CertificateRules.DetermineState(certificate, Now);

        Assert.Equal(CertificateState.ExpiringSoon, result);
    }

    [Fact]
    public void DetermineState_PastExpiry_ReturnsExpired()
    {
        var certificate = new OfflineCertificate("subject-1", "thumb-1", Now.AddDays(-400), Now.AddDays(-1));

        var result = CertificateRules.DetermineState(certificate, Now);

        Assert.Equal(CertificateState.Expired, result);
    }

    [Fact]
    public void DetermineState_ExpiringExactlyNow_ReturnsExpired()
    {
        var certificate = new OfflineCertificate("subject-1", "thumb-1", Now.AddDays(-365), Now);

        var result = CertificateRules.DetermineState(certificate, Now);

        Assert.Equal(CertificateState.Expired, result);
    }
}
