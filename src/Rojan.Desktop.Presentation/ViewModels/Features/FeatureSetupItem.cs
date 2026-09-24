using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Features;

/// <summary>
/// PASS D10: one togglable row in <see cref="FeatureSetupWindowViewModel"/>'s first-run list or
/// <c>Settings.SettingsPageViewModel</c>'s personalization section - both build this from the exact
/// same source (<c>Modules.IModuleRegistry</c>, filtered to <c>Mandatory</c>/<c>Optional</c>), so the
/// two screens can never show a different feature list. A mandatory feature is still represented here
/// (shown fixed/checked, never actually togglable) rather than filtered out entirely - the product
/// requirement is "mandatory features visibly fixed/enabled," not "hidden from the list."
/// </summary>
public sealed class FeatureSetupItem : ViewModelBase
{
    private bool _isSelected;

    public FeatureSetupItem(string featureKey, string title, string iconGlyph, bool isMandatory, bool isSelected)
    {
        FeatureKey = featureKey;
        Title = title;
        IconGlyph = iconGlyph;
        IsMandatory = isMandatory;
        _isSelected = isSelected;
    }

    /// <summary>The stable module id - never a localized display string, so persistence survives a language change.</summary>
    public string FeatureKey { get; }

    public string Title { get; }

    public string IconGlyph { get; }

    /// <summary>True for a core-infrastructure feature (Dashboard/Settings) - its row's toggle is disabled and always shows checked, but it is still represented in the list for transparency.</summary>
    public bool IsMandatory { get; }

    /// <summary>Bound by the row's toggle - for a mandatory item this is always true and the control itself is disabled, so nothing ever writes false here for one.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
