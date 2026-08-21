using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1): PNG <see cref="byte"/>[]
/// (as returned by <c>Application.Salons.ISalonQueryService.GetSalonQrCodeAsync</c>/
/// <c>Application.Membership.ISalonInviteService.GetInviteQrCodeAsync</c>/
/// <c>Application.QrCodes.IStaticQrCodeGenerator</c>) to a displayable
/// <see cref="BitmapImage"/> - the one WPF-specific conversion step that
/// keeps <c>ViewModels.QrCodes.QrCodesPageViewModel</c> itself free of
/// WPF imaging types, same "ViewModels stay testable without a UI thread"
/// reasoning <c>Mvvm.ViewModelBase</c>'s own doc comment gives.
/// <see cref="BitmapCacheOption.OnLoad"/> + <c>Freeze()</c> so the
/// returned image is immediately cross-thread-safe and the backing
/// <see cref="MemoryStream"/> can be disposed before this method returns.
/// </summary>
public sealed class ByteArrayToBitmapImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes)
        {
            return null;
        }

        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("ByteArrayToBitmapImageConverter is one-way (display only).");
}
