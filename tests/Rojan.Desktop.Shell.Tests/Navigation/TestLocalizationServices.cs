using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// UX toolbar redesign: <see cref="MainWindowViewModel"/>'s new
/// <see cref="ILocalizationService"/> constructor parameter (header
/// Language selector), factored out here the same way
/// <see cref="TestThemeServices"/> stubs out constructor dependencies
/// these navigation/branch-switcher tests don't exercise directly.
/// </summary>
internal static class TestLocalizationServices
{
    public static ILocalizationService Service { get; } = new StubLocalizationService();

    private sealed class StubLocalizationService : ILocalizationService
    {
        private static readonly LanguageInfo Persian = new("fa-IR", "فارسی", "Persian", true, "Vazirmatn", NumberDigits.Persian, "IRR", "PersianDateProvider", "1.0", "1.0", true);

        public LanguageInfo CurrentLanguage => Persian;

        public IReadOnlyList<LanguageInfo> AvailableLanguages { get; } = [Persian];

        public bool IsRestartRequired => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
