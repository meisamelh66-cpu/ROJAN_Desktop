using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Settings;

/// <summary>
/// Drives SettingsPage's Language section - the Phase 19A reference
/// migration's proving ground for the whole localization platform:
/// picking a built-in language or an installed pack, applying it
/// (persists + flags <see cref="IsRestartRequired"/> - language changes
/// take effect on next launch, never live), and the "Available
/// Languages" foundation (Download/Install/Remove/Update) which
/// <see cref="ILanguagePackRepository"/> backs with an always-empty
/// catalog for now, per Phase 19A's "Do NOT connect to servers yet"
/// instruction - every action there reports
/// <see cref="Strings.Settings_Language_ComingSoon"/> rather than
/// silently doing nothing.
/// </summary>
public sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly ILocalizationService _localizationService;
    private readonly ILanguagePackRepository _packRepository;

    private LanguageInfo? _selectedLanguage;
    private string _statusMessage = string.Empty;

    public SettingsPageViewModel(ILocalizationService localizationService, ILanguagePackRepository packRepository)
    {
        _localizationService = localizationService;
        _packRepository = packRepository;

        BuiltInLanguages = new ObservableCollection<LanguageInfo>(
            localizationService.AvailableLanguages.Where(language => language.IsBuiltIn));
        InstalledPacks = new ObservableCollection<LanguageInfo>(
            localizationService.AvailableLanguages.Where(language => !language.IsBuiltIn));
        AvailableLanguagePacks = new ObservableCollection<LanguagePackCatalogEntry>();

        SelectedLanguage = localizationService.AvailableLanguages.FirstOrDefault(language => language.Code == localizationService.CurrentLanguage.Code);

        ApplyLanguageCommand = new AsyncRelayCommand(_ => ApplyLanguageAsync(), _ => SelectedLanguage is not null);
        RestartCommand = new RelayCommand(_ => Restart());
        RefreshAvailablePacksCommand = new AsyncRelayCommand(_ => RefreshAvailablePacksAsync());
        DownloadOrInstallCommand = new AsyncRelayCommand(parameter => DownloadOrInstallAsync(parameter as LanguagePackCatalogEntry));
        RemovePackCommand = new AsyncRelayCommand(parameter => RemovePackAsync(parameter as LanguageInfo));

        _ = RefreshAvailablePacksAsync();
    }

    public ObservableCollection<LanguageInfo> BuiltInLanguages { get; }

    public ObservableCollection<LanguageInfo> InstalledPacks { get; }

    public ObservableCollection<LanguagePackCatalogEntry> AvailableLanguagePacks { get; }

    public ICommand ApplyLanguageCommand { get; }

    public ICommand RestartCommand { get; }

    public ICommand RefreshAvailablePacksCommand { get; }

    public ICommand DownloadOrInstallCommand { get; }

    public ICommand RemovePackCommand { get; }

    public LanguageInfo? SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    public LanguageInfo CurrentLanguage => _localizationService.CurrentLanguage;

    public string CurrentLanguageDisplay =>
        Strings.Settings_Language_CurrentFormat.Replace("{0}", CurrentLanguage.NativeName, StringComparison.Ordinal);

    public bool IsRestartRequired => _localizationService.IsRestartRequired;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private async Task ApplyLanguageAsync()
    {
        if (SelectedLanguage is null)
        {
            return;
        }

        await _localizationService.SetLanguageAsync(SelectedLanguage.Code).ConfigureAwait(true);
        OnPropertyChanged(nameof(IsRestartRequired));
        StatusMessage = _localizationService.IsRestartRequired ? Strings.Settings_Language_RestartRequired : string.Empty;
    }

    private static void Restart()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(executablePath))
        {
            Process.Start(executablePath);
        }

        System.Windows.Application.Current.Shutdown();
    }

    private async Task RefreshAvailablePacksAsync()
    {
        var catalog = await _packRepository.GetAvailableLanguagePacksAsync().ConfigureAwait(true);
        AvailableLanguagePacks.Clear();
        foreach (var entry in catalog)
        {
            AvailableLanguagePacks.Add(entry);
        }
    }

    private async Task DownloadOrInstallAsync(LanguagePackCatalogEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            await _packRepository.DownloadAndInstallAsync(entry.Code).ConfigureAwait(true);
        }
        catch (NotSupportedException exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task RemovePackAsync(LanguageInfo? language)
    {
        if (language is null)
        {
            return;
        }

        try
        {
            await _packRepository.RemovePackAsync(language.Code).ConfigureAwait(true);
        }
        catch (NotSupportedException exception)
        {
            StatusMessage = exception.Message;
        }
    }
}
