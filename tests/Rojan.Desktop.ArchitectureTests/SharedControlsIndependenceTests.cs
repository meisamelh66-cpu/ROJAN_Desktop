using NetArchTest.Rules;
using Rojan.Desktop.Presentation.Controls.Shared;

namespace Rojan.Desktop.ArchitectureTests;

/// <summary>
/// Enforces the "Controls.Shared is genuinely shared" boundary the Sprint 1
/// "ROJAN Premium Kit" plan called for: a control living in Controls.Shared
/// must not quietly couple back to a single module's ViewModel or private
/// Controls subfolder, or the "shared" name stops being true. One known,
/// pre-existing exception is carved out explicitly below rather than
/// silently ignored - see that test's own doc comment.
/// </summary>
public sealed class SharedControlsIndependenceTests
{
    [Fact]
    public void SharedControls_ShouldNotDependOnViewModels()
    {
        var result = Types.InAssembly(typeof(StatTile).Assembly)
            .That()
            .ResideInNamespace("Rojan.Desktop.Presentation.Controls.Shared")
            .ShouldNot()
            .HaveDependencyOnAny("Rojan.Desktop.Presentation.ViewModels")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// KPIValue is a documented, known exception: Sprint 1 Commit 2 moved it
    /// into Controls.Shared but its count-up-animation number parsing still
    /// calls Controls.Dashboard.KpiNumberParsing, a helper that was left
    /// behind since only the 5 named components were in scope for that move
    /// (see the Commit 2 report). Every other Controls.Shared type is held
    /// to the full rule; KPIValue is excluded here rather than silently
    /// exempted, so this test still catches any *new* coupling to a single
    /// module's private Controls subfolder.
    /// </summary>
    [Fact]
    public void SharedControls_ExceptKnownException_ShouldNotDependOnSingleModuleControlNamespaces()
    {
        var result = Types.InAssembly(typeof(StatTile).Assembly)
            .That()
            .ResideInNamespace("Rojan.Desktop.Presentation.Controls.Shared")
            .And()
            .DoNotHaveName("KPIValue")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Rojan.Desktop.Presentation.Controls.Dashboard",
                "Rojan.Desktop.Presentation.Controls.Analytics",
                "Rojan.Desktop.Presentation.Controls.Notifications",
                "Rojan.Desktop.Presentation.Controls.Search",
                "Rojan.Desktop.Presentation.Controls.Help")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
