using Rojan.Desktop.Presentation.Controls.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

/// <summary>Page Stability: the shared "initial load vs refresh" rule every page/profile ViewModel uses, and the DashboardWidget rule that decides when an ErrorMessage is shown inline above still-visible content instead of replacing it.</summary>
public sealed class DashboardStateExtensionsTests
{
    [Theory]
    [InlineData(DashboardState.Loaded, true)]
    [InlineData(DashboardState.Empty, true)]
    [InlineData(DashboardState.Loading, false)]
    [InlineData(DashboardState.Error, false)]
    public void HasSettledResult_OnlyLoadedOrEmptyCountAsContentAlreadyOnScreen(DashboardState state, bool expected)
    {
        Assert.Equal(expected, state.HasSettledResult());
    }

    [Theory]
    [InlineData(DashboardState.Loaded, "Something went wrong", true)]
    [InlineData(DashboardState.Empty, "Something went wrong", true)]
    [InlineData(DashboardState.Loaded, null, false)]
    [InlineData(DashboardState.Loaded, "", false)]
    [InlineData(DashboardState.Error, "Something went wrong", false)] // the full ErrorPanel handles this one
    [InlineData(DashboardState.Loading, "Something went wrong", false)]
    public void DashboardWidget_ComputeHasInlineError_OnlyForAFailedRefreshOverSettledContent(DashboardState state, string? errorMessage, bool expected)
    {
        Assert.Equal(expected, DashboardWidget.ComputeHasInlineError(state, errorMessage));
    }
}
