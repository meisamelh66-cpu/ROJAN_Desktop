using Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Domain.Tests.Reporting;

public sealed class TrendCalculatorTests
{
    [Fact]
    public void ComputeTrend_WhenCurrentGreaterThanPrevious_ReturnsUp()
    {
        Assert.Equal(TrendDirection.Up, TrendCalculator.ComputeTrend(150m, 100m));
    }

    [Fact]
    public void ComputeTrend_WhenCurrentLessThanPrevious_ReturnsDown()
    {
        Assert.Equal(TrendDirection.Down, TrendCalculator.ComputeTrend(80m, 100m));
    }

    [Fact]
    public void ComputeTrend_WhenCurrentEqualsPrevious_ReturnsFlat()
    {
        Assert.Equal(TrendDirection.Flat, TrendCalculator.ComputeTrend(100m, 100m));
    }

    [Fact]
    public void ComputeTrend_WhenDifferenceIsNegligible_ReturnsFlat()
    {
        Assert.Equal(TrendDirection.Flat, TrendCalculator.ComputeTrend(100.00001m, 100m));
    }

    [Fact]
    public void ComputeChangePercent_WithPositiveChange_ReturnsCorrectPercentage()
    {
        Assert.Equal(50m, TrendCalculator.ComputeChangePercent(150m, 100m));
    }

    [Fact]
    public void ComputeChangePercent_WithNegativeChange_ReturnsCorrectNegativePercentage()
    {
        Assert.Equal(-20m, TrendCalculator.ComputeChangePercent(80m, 100m));
    }

    [Fact]
    public void ComputeChangePercent_WithZeroPreviousAndNonZeroCurrent_ReturnsOneHundred()
    {
        Assert.Equal(100m, TrendCalculator.ComputeChangePercent(50m, 0m));
    }

    [Fact]
    public void ComputeChangePercent_WithZeroPreviousAndZeroCurrent_ReturnsZero()
    {
        Assert.Equal(0m, TrendCalculator.ComputeChangePercent(0m, 0m));
    }

    [Fact]
    public void ComputeChangePercent_RoundsToOneDecimalPlace()
    {
        // (4 - 3) / 3 * 100 = 33.333...% -> rounds to 33.3.
        Assert.Equal(33.3m, TrendCalculator.ComputeChangePercent(4m, 3m));
    }
}
