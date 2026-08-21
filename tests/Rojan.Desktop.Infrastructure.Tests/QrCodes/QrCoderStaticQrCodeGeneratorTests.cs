using Rojan.Desktop.Infrastructure.QrCodes;

namespace Rojan.Desktop.Infrastructure.Tests.QrCodes;

/// <summary>Exercises <see cref="QrCoderStaticQrCodeGenerator"/> against the real QRCoder library - no transport/HTTP concern to fake here, unlike every <c>Backend*Repository</c> test, since this class never talks to the network.</summary>
public sealed class QrCoderStaticQrCodeGeneratorTests
{
    [Fact]
    public void GeneratePng_ReturnsANonEmptyValidPngByteArray()
    {
        var sut = new QrCoderStaticQrCodeGenerator();

        var bytes = sut.GeneratePng("https://rojanai.ir/download/manager", 512);

        Assert.NotEmpty(bytes);
        // PNG file signature: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], bytes[..8]);
    }

    [Fact]
    public void GeneratePng_DifferentUrls_ProduceDifferentBytes()
    {
        var sut = new QrCoderStaticQrCodeGenerator();

        var first = sut.GeneratePng("https://rojanai.ir/download/manager", 512);
        var second = sut.GeneratePng("https://rojanai.ir/s/some-other-salon", 512);

        Assert.NotEqual(first, second);
    }
}
