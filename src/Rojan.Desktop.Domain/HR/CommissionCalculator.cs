namespace Rojan.Desktop.Domain.HR;

/// <summary>Commission math - same "Domain business math, composed in Application" pattern as <c>Domain.Accounting.InvoiceCalculator</c>.</summary>
public static class CommissionCalculator
{
    public static decimal ComputeCommission(CommissionRule rule, decimal grossAmount) => rule.Type switch
    {
        CommissionType.Percentage => Math.Round(grossAmount * rule.Value, 2, MidpointRounding.AwayFromZero),
        CommissionType.FixedAmount => rule.Value,
        _ => 0m,
    };
}
