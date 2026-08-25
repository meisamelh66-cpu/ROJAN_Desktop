using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.BookingWorkflow;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Booking Intelligence Phase 1 (Smart Specialist Ordering): true when the
/// bound specialist row has a real, explicit assignment to the wizard's
/// currently-selected service - backs the Specialist step's "Assigned"
/// badge. Needs an <see cref="IMultiValueConverter"/> for the same reason
/// <see cref="SeverityFilterChipSelectedConverter"/> does: the check
/// depends on two independent bindings (the row's own <c>DataContext</c>
/// and the wizard's <c>SelectedService</c>), not one. Mirrors
/// <c>BookingWizardViewModel.IsExplicitlyAssignedToSelectedService</c>'s
/// own logic exactly - duplicated here only because it is a two-line,
/// stable predicate and this converter must stay self-contained XAML
/// plumbing, not a callback into the ViewModel.
/// </summary>
public sealed class SpecialistAssignedToServiceConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [WorkflowSpecialistOptionDto specialist, WorkflowServiceOptionDto selectedService]
        && specialist.AssignedServiceIds.Count > 0
        && specialist.AssignedServiceIds.Contains(selectedService.Id)
            ? "True"
            : "False";

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("SpecialistAssignedToServiceConverter is one-way (display only).");
}
