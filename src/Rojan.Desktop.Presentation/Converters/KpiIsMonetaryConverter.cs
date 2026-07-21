using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Presentation.Controls.Dashboard;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase 35: maps a KPI's stable Id to whether it's a monetary amount that KPIValue should mask by default - see KpiPrivacy for the actual list.</summary>
public sealed class KpiIsMonetaryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        KpiPrivacy.IsMonetary(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("KpiIsMonetaryConverter is one-way (display only).");
}
