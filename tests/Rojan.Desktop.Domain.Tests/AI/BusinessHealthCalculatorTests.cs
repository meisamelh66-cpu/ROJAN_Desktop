using Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Domain.Tests.AI;

public sealed class BusinessHealthCalculatorTests
{
    [Fact]
    public void ComputeOverallScore_WithNoComponents_ReturnsZero()
    {
        Assert.Equal(0m, BusinessHealthCalculator.ComputeOverallScore([]));
    }

    [Fact]
    public void ComputeOverallScore_ComputesWeightedAverage()
    {
        IReadOnlyList<BusinessHealthComponent> components =
        [
            new(InsightCategory.Revenue, "Revenue", 80m, 0.5m),
            new(InsightCategory.Attendance, "Attendance", 60m, 0.5m),
        ];

        Assert.Equal(70m, BusinessHealthCalculator.ComputeOverallScore(components));
    }

    [Fact]
    public void ComputeOverallScore_ClampsAboveOneHundred()
    {
        IReadOnlyList<BusinessHealthComponent> components = [new(InsightCategory.Revenue, "Revenue", 150m, 1m)];

        Assert.Equal(100m, BusinessHealthCalculator.ComputeOverallScore(components));
    }

    [Fact]
    public void ComputeOverallScore_ClampsBelowZero()
    {
        IReadOnlyList<BusinessHealthComponent> components = [new(InsightCategory.Revenue, "Revenue", -20m, 1m)];

        Assert.Equal(0m, BusinessHealthCalculator.ComputeOverallScore(components));
    }

    [Fact]
    public void ComputeOverallScore_WhenTotalWeightIsZero_ReturnsZero()
    {
        IReadOnlyList<BusinessHealthComponent> components = [new(InsightCategory.Revenue, "Revenue", 80m, 0m)];

        Assert.Equal(0m, BusinessHealthCalculator.ComputeOverallScore(components));
    }

    [Fact]
    public void ComputeOverallScore_RoundsToOneDecimalPlace()
    {
        IReadOnlyList<BusinessHealthComponent> components =
        [
            new(InsightCategory.Revenue, "Revenue", 100m, 1m),
            new(InsightCategory.Customer, "Customer", 0m, 1m),
            new(InsightCategory.Inventory, "Inventory", 0m, 1m),
        ];

        Assert.Equal(33.3m, BusinessHealthCalculator.ComputeOverallScore(components));
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(150, 100)]
    [InlineData(42.5, 42.5)]
    public void NormalizeToScore_ClampsToZeroToOneHundredRange(decimal rawValue, decimal expected)
    {
        Assert.Equal(expected, BusinessHealthCalculator.NormalizeToScore(rawValue));
    }
}
