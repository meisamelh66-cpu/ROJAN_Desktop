namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Genuine Domain math behind the KPI Engine - derives a <see cref="TrendDirection"/> and percentage change from a current value compared against its prior-period value.</summary>
public static class TrendCalculator
{
    private const decimal FlatThreshold = 0.0001m;

    public static TrendDirection ComputeTrend(decimal currentValue, decimal previousValue)
    {
        var change = currentValue - previousValue;
        if (Math.Abs(change) < FlatThreshold)
        {
            return TrendDirection.Flat;
        }

        return change > 0 ? TrendDirection.Up : TrendDirection.Down;
    }

    /// <summary>Percentage change from <paramref name="previousValue"/> to <paramref name="currentValue"/>. A zero prior value with a non-zero current value is treated as a full 100% increase rather than an undefined division.</summary>
    public static decimal ComputeChangePercent(decimal currentValue, decimal previousValue)
    {
        if (previousValue == 0m)
        {
            return currentValue == 0m ? 0m : 100m;
        }

        return Math.Round((currentValue - previousValue) / Math.Abs(previousValue) * 100m, 1);
    }
}
