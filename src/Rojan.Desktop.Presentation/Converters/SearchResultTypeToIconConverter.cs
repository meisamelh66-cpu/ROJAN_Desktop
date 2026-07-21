using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Maps a <see cref="SearchResultType"/> directly to its Fluent icon
/// glyph character - reuses the exact codepoints already assigned to
/// each module's own sidebar icon in <c>Themes/Icons.xaml</c>
/// (<c>Rojan.Icon.Customers</c>/<c>Bookings</c>/<c>Specialists</c>/
/// <c>Services</c>/<c>Inventory</c>), so no new icon glyph is introduced
/// for those five - only <see cref="SearchResultType.Page"/> and
/// <see cref="SearchResultType.Command"/> fall back to the existing
/// generic Menu/Search glyphs, since neither has one obvious module icon
/// of its own. A converter cannot resolve a <c>{StaticResource}</c> key
/// bound at runtime, so this mirrors the codepoints directly, as
/// <c>NotificationSeverityToIconConverter</c> already does - keep both
/// in sync with <c>Icons.xaml</c> if either changes.
/// </summary>
public sealed class SearchResultTypeToIconConverter : IValueConverter
{
    private const char MenuGlyph = (char)0xE700;
    private const char CustomersGlyph = (char)0xE77B;
    private const char BookingsGlyph = (char)0xE8BF;
    private const char SpecialistsGlyph = (char)0xE716;
    private const char ServicesGlyph = (char)0xE9D9;
    private const char InventoryGlyph = (char)0xE71D;
    private const char SearchGlyph = (char)0xE721;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SearchResultType.Customer => CustomersGlyph.ToString(),
        SearchResultType.Booking => BookingsGlyph.ToString(),
        SearchResultType.Specialist => SpecialistsGlyph.ToString(),
        SearchResultType.Service => ServicesGlyph.ToString(),
        SearchResultType.Product => InventoryGlyph.ToString(),
        SearchResultType.Command => SearchGlyph.ToString(),
        _ => MenuGlyph.ToString(),
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("SearchResultTypeToIconConverter is one-way (display only).");
}
