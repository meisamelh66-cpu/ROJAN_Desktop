namespace Rojan.Desktop.Domain.AI;

/// <summary>Genuine Domain math behind the Business Health Score - a weighted average of its components, clamped to 0-100.</summary>
public static class BusinessHealthCalculator
{
    public static decimal ComputeOverallScore(IReadOnlyList<BusinessHealthComponent> components)
    {
        if (components.Count == 0)
        {
            return 0m;
        }

        var totalWeight = components.Sum(c => c.Weight);
        if (totalWeight <= 0m)
        {
            return 0m;
        }

        var weightedSum = components.Sum(c => c.Score * c.Weight);
        var score = weightedSum / totalWeight;
        return Math.Round(Math.Clamp(score, 0m, 100m), 1);
    }

    /// <summary>Maps a 0-100 metric ratio (e.g. attendance rate already 0-100, or a trend-change-percent normalized elsewhere) to a component score, clamped so a single wild outlier can't blow out the composite.</summary>
    public static decimal NormalizeToScore(decimal rawPercentValue) => Math.Clamp(rawPercentValue, 0m, 100m);
}
