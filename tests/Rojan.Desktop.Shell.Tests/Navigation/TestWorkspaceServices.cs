using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.Workspaces;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// The real <see cref="WorkspaceService"/> over an empty in-memory
/// repository - <see cref="MainWindowViewModel"/>'s 3 Workspace &amp;
/// Window Management constructor parameters, factored out here since none
/// of these navigation/branch-switcher tests exercise workspace behavior
/// directly, the same reasoning <c>TestHelpServices</c>/
/// <c>TestSearchServices</c> already establish.
/// </summary>
internal static class TestWorkspaceServices
{
    public static IWorkspaceService CreateService() => new WorkspaceService(new StubWorkspaceRepository());

    public static IFloatingWindowManager FloatingWindowManager { get; } = new StubFloatingWindowManager();

    /// <summary>An empty container - <c>WorkspaceHostViewModel</c> only resolves tab content from this when a secondary pane actually exists, which none of these tests create.</summary>
    public static IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
}
