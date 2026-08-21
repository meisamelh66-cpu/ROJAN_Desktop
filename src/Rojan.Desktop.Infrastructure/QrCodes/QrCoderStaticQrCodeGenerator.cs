using QRCoder;
using Rojan.Desktop.Application.QrCodes;

namespace Rojan.Desktop.Infrastructure.QrCodes;

/// <summary>QRCoder-backed <see cref="IStaticQrCodeGenerator"/> - MIT-licensed, pure C#, no <c>System.Drawing</c>/GDI+ dependency (<see cref="PngByteQRCode"/> renders the PNG bytes directly), which is why this package rather than a GDI+-based alternative was chosen.</summary>
public sealed class QrCoderStaticQrCodeGenerator : IStaticQrCodeGenerator
{
    public byte[] GeneratePng(string url, int sizePx)
    {
        var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        var pngQrCode = new PngByteQRCode(data);
        var moduleCount = data.ModuleMatrix.Count;
        var pixelsPerModule = Math.Max(1, sizePx / moduleCount);

        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}
