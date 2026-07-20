using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Maps an <see cref="ActivityEntryDto"/>'s stable <c>Id</c> (e.g.
/// "activity-1") to a localized description, mirroring
/// <see cref="KpiLabelConverter"/>'s reasoning: FakeDashboardRepository
/// (Infrastructure) returns English text and cannot depend on
/// Presentation's Strings, so localization happens here at the View
/// boundary. Unrecognized ids fall back to the repository-provided
/// Description as-is.
/// </summary>
public sealed class ActivityDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ActivityEntryDto activity)
        {
            return string.Empty;
        }

        return activity.Id switch
        {
            "activity-1" => Strings.Dashboard_Activity_NewBooking,
            "activity-2" => Strings.Dashboard_Activity_ProfileUpdated,
            "activity-3" => Strings.Dashboard_Activity_PaymentReceived,
            "activity-4" => Strings.Dashboard_Activity_TaskCompleted,
            _ => activity.Description,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("ActivityDescriptionConverter is one-way (display only).");
}
