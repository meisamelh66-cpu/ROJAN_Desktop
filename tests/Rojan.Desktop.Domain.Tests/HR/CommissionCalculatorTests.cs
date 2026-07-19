using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Domain.Tests.HR;

public sealed class CommissionCalculatorTests
{
    [Fact]
    public void ComputeCommission_Percentage_RoundsToTwoDecimalPlacesAwayFromZero()
    {
        var rule = new CommissionRule("rule-1", "employee-1", "Jordan Lee", CommissionType.Percentage, 0.15m, string.Empty);

        var result = CommissionCalculator.ComputeCommission(rule, 89.64m);

        Assert.Equal(13.45m, result);
    }

    [Fact]
    public void ComputeCommission_FixedAmount_ReturnsRuleValueRegardlessOfGrossAmount()
    {
        var rule = new CommissionRule("rule-4", "employee-4", "Riley Chen", CommissionType.FixedAmount, 15m, string.Empty);

        var result = CommissionCalculator.ComputeCommission(rule, 500m);

        Assert.Equal(15m, result);
    }
}
