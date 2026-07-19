using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Reporting;

/// <summary>One editable row of the Filter Panel - a generic (FilterType, Value) pair, matching the Filter Engine's generic <see cref="ReportFilterDto"/> shape so the same UI covers all eight <see cref="FilterType"/>s without a bespoke control per type.</summary>
public sealed class FilterEntryViewModel : ViewModelBase
{
    private FilterType _filterType;
    private string _value = string.Empty;

    public FilterEntryViewModel(FilterType filterType)
    {
        _filterType = filterType;
    }

    public FilterType FilterType
    {
        get => _filterType;
        set => SetProperty(ref _filterType, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public ReportFilterDto ToDto() => new(FilterType, Value, $"{FilterType}: {Value}");
}
