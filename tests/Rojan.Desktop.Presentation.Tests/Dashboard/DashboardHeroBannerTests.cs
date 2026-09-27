using Rojan.Desktop.Presentation.Controls.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

public sealed class DashboardHeroBannerTests
{
    [Theory]
    [InlineData(400, 160)]  // narrow: clamped to the minimum
    [InlineData(1000, 250)] // about the Dashboard content width at 1366x768: exactly 16:4
    [InlineData(1200, 300)]
    [InlineData(1600, 300)] // about the content width at 1920x1080: capped at the maximum
    public void ComputeHeight_IsSixteenToFour_WithinTheClamp(double width, double expected)
    {
        Assert.Equal(expected, DashboardHeroBanner.ComputeHeight(width), 3);
    }
}
