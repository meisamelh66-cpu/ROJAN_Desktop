namespace Rojan.Desktop.Domain.HR;

public static class PayrollCalculator
{
    public static decimal ComputeNetSalary(decimal baseSalary, decimal commissionTotal, decimal bonus, decimal deduction) =>
        baseSalary + commissionTotal + bonus - deduction;
}
