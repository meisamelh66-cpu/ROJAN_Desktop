using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Maps a <see cref="SearchResultType"/> to its localized display label (<c>Strings.Search_Type_*</c>).</summary>
public sealed class SearchResultTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SearchResultType.Customer => Strings.Search_Type_Customer,
        SearchResultType.Booking => Strings.Search_Type_Booking,
        SearchResultType.Specialist => Strings.Search_Type_Specialist,
        SearchResultType.Service => Strings.Search_Type_Service,
        SearchResultType.Product => Strings.Search_Type_Product,
        SearchResultType.Command => Strings.Search_Type_Command,
        _ => Strings.Search_Type_Page,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("SearchResultTypeToLabelConverter is one-way (display only).");
}
