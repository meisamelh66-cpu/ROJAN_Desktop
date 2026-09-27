using Rojan.Desktop.Presentation.Views.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

public sealed class DashboardPageLayoutTests
{
    // Approximate Dashboard content widths (window minus sidebar and page margins) at the target
    // resolutions, plus narrow widths that must fall back gracefully.
    [Theory]
    [InlineData(1600, 6)] // 1920x1080
    [InlineData(1280, 3)] // 1600x900
    [InlineData(1040, 3)] // 1366x768
    [InlineData(600, 2)]
    [InlineData(360, 1)]
    public void ComputeKpiColumns_IsAlwaysADivisorOfTheSixCards(double width, int expected)
    {
        var columns = DashboardPage.ComputeKpiColumns(width);

        Assert.Equal(expected, columns);
        Assert.Equal(0, 6 % columns);
    }

    [Theory]
    [InlineData(1600, false)]
    [InlineData(1040, false)]
    [InlineData(899, true)]
    public void IsCompactWidth_RestacksCardRowsOnlyBelowTheThreshold(double width, bool expected)
    {
        Assert.Equal(expected, DashboardPage.IsCompactWidth(width));
    }
}
