using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Domain.Tests.HR;

public sealed class PayrollCalculatorTests
{
    [Fact]
    public void ComputeNetSalary_AddsBaseAndCommissionAndBonusMinusDeduction()
    {
        var result = PayrollCalculator.ComputeNetSalary(3200m, 450m, 100m, 50m);

        Assert.Equal(3700m, result);
    }

    [Fact]
    public void ComputeNetSalary_NoCommissionBonusOrDeduction_ReturnsBaseSalary()
    {
        var result = PayrollCalculator.ComputeNetSalary(2000m, 0m, 0m, 0m);

        Assert.Equal(2000m, result);
    }
}
