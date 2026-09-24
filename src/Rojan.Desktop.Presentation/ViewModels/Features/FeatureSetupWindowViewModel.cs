using System.Windows.Input;
using Rojan.Desktop.Presentation.Features;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Features;

/// <summary>
/// PASS D10 (Feature Personalization &amp; Modular Workspace): <c>FeatureSetupWindow</c>'s
/// DataContext - shown once, before <c>MainWindow</c>, only when the current salon has no saved
/// feature configuration yet (<c>IFeatureConfigurationService.HasConfiguration</c> is false).
/// Constructed via <c>new</c> by its opener (same "constructed-via-new-by-its-opener" shape
/// <c>SalonSelectionWindowViewModel</c> already establishes), not DI-resolved - <see cref="Items"/> is
/// built from the real, already-resolved module registry no constructor-injected dependency alone
/// could supply pre-filtered.
///
/// "Closing/canceling must not corrupt configuration" (Phase 5): <c>IFeatureConfigurationService.SaveEnabledFeatureKeysAsync</c>
/// is called from exactly one place - <see cref="ContinueCommand"/> - so a window closed via the title
/// bar or Alt+F4 without pressing Continue never persists anything; the opener (<c>Shell.App.OnStartup</c>)
/// treats that exactly like declining the Login/Salon Selection gates before it - a clean shutdown, not
/// a partially-configured app.
/// </summary>
public sealed class FeatureSetupWindowViewModel : ViewModelBase
{
    private readonly IFeatureConfigurationService _featureConfigurationService;
    private bool _isBusy;

    public FeatureSetupWindowViewModel(IFeatureConfigurationService featureConfigurationService, IReadOnlyList<ModuleDescriptor> modules)
    {
        _featureConfigurationService = featureConfigurationService;

        Items = modules
            .Where(descriptor => descriptor.Metadata.FeatureConfigurability is FeatureConfigurability.Mandatory or FeatureConfigurability.Optional)
            .OrderBy(descriptor => descriptor.Metadata.Order)
            .Select(descriptor => new FeatureSetupItem(
                descriptor.Metadata.Id,
                descriptor.Metadata.Title,
                descriptor.Metadata.IconGlyph,
                isMandatory: descriptor.Metadata.FeatureConfigurability == FeatureConfigurability.Mandatory,
                isSelected: descriptor.Metadata.FeatureConfigurability == FeatureConfigurability.Mandatory || descriptor.Metadata.DefaultFeatureEnabled))
            .ToList();

        ContinueCommand = new AsyncRelayCommand(_ => ContinueAsync());
    }

    /// <summary>Every <c>Mandatory</c>/<c>Optional</c> module from the real registry, in display order - mandatory rows pre-checked and fixed, optional rows pre-checked per <see cref="ModuleMetadata.DefaultFeatureEnabled"/> so a brand-new salon starts from a sensible, usable default rather than an empty workspace.</summary>
    public IReadOnlyList<FeatureSetupItem> Items { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ICommand ContinueCommand { get; }

    /// <summary>Raised once the chosen selection has been saved - <c>FeatureSetupWindow</c> subscribes to this to know when to close, same shape as <c>SalonSelectionWindowViewModel.Selected</c>.</summary>
    public event EventHandler? Completed;

    private async Task ContinueAsync()
    {
        IsBusy = true;
        try
        {
            // Mandatory rows are never written - IFeatureConfigurationService.EnabledFeatureKeys is
            // only ever consulted for Optional modules (see MainWindowViewModel.BuildVisibleNavigationItems),
            // so persisting them would be meaningless, not merely redundant.
            var enabledKeys = Items
                .Where(item => !item.IsMandatory && item.IsSelected)
                .Select(item => item.FeatureKey)
                .ToHashSet();

            await _featureConfigurationService.SaveEnabledFeatureKeysAsync(enabledKeys).ConfigureAwait(true);
            Completed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
