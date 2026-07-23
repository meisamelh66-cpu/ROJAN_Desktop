using Rojan.Desktop.Presentation.Theming;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Reference-parity Phase A: <see cref="MainWindowViewModel"/>'s new
/// <see cref="IThemeService"/> constructor parameter (header theme
/// toggle), factored out here the same way <c>TestSearchServices</c>/
/// <c>TestWorkspaceServices</c> stub out constructor dependencies these
/// navigation/branch-switcher tests don't exercise directly.
/// </summary>
internal static class TestThemeServices
{
    public static IThemeService Service { get; } = new StubThemeService();

    private sealed class StubThemeService : IThemeService
    {
        public ThemeMode SelectedMode => ThemeMode.Light;

        public ThemeMode ResolvedTheme => ThemeMode.Light;

        public bool IsRestartRequired => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
