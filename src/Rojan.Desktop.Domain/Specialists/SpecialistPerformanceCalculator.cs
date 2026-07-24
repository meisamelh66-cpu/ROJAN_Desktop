namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// Specialist Intelligence (Sprint 5 Commit 5A): genuine Domain math behind
/// a specialist's performance score/level/recommendation signal - the same
/// "Domain business math, composed in Application" pattern
/// <c>HR.CommissionCalculator</c>/<c>AI.BusinessHealthCalculator</c>
/// already establish. Every method here is pure and takes its inputs as
/// parameters (<see cref="SpecialistPerformanceIndicators"/>, an already-known
/// <see cref="SpecialistStatus"/>) rather than fetching them - Domain owns
/// the definition and the calculation, never the data source. Negative
/// counts are treated as zero rather than rejected, the same "degrade
/// gracefully, never throw on a degenerate input" convention
/// <c>AI.BusinessHealthCalculator.ComputeOverallScore</c> already follows
/// for a zero total weight.
/// </summary>
public static class SpecialistPerformanceCalculator
{
    /// <summary>
    /// Completed bookings earn points, no-shows and cancellations cost
    /// points (a no-show costs three times as much as a cancellation - a
    /// specialist has no control over a client who simply doesn't show up
    /// to blame them for, but the score still needs to reflect that no
    /// value was delivered that slot), clamped to [0, 100] so the result
    /// always reads as a percentage-like figure regardless of how many
    /// bookings a long-tenured specialist accumulates.
    /// </summary>
    public static int ComputePerformanceScore(SpecialistPerformanceIndicators indicators)
    {
        var completed = Math.Max(0, indicators.CompletedBookingCount);
        var cancelled = Math.Max(0, indicators.CancelledBookingCount);
        var noShow = Math.Max(0, indicators.NoShowBookingCount);

        var score = (completed * 10) - (cancelled * 5) - (noShow * 15);
        return Math.Clamp(score, 0, 100);
    }

    /// <summary>Excellent (80+) -&gt; Good (50+) -&gt; NeedsImprovement (20+) -&gt; Underperforming (below 20), purely a function of <see cref="ComputePerformanceScore"/>'s result.</summary>
    public static SpecialistPerformanceLevel ClassifyPerformance(int score) => score switch
    {
        >= 80 => SpecialistPerformanceLevel.Excellent,
        >= 50 => SpecialistPerformanceLevel.Good,
        >= 20 => SpecialistPerformanceLevel.NeedsImprovement,
        _ => SpecialistPerformanceLevel.Underperforming,
    };

    /// <summary>
    /// Combines the calculated <paramref name="level"/> with the
    /// specialist's actual <paramref name="status"/> (<see cref="SpecialistRules"/>
    /// already owns what that status means/how it may change - this only
    /// reads it): an <see cref="SpecialistStatus.Inactive"/> specialist
    /// always needs Attention regardless of how strong their past
    /// performance was, an <see cref="SpecialistStatus.OnLeave"/> one is
    /// always worth Monitoring, and only an Active specialist's signal is
    /// driven purely by <paramref name="level"/>.
    /// </summary>
    public static SpecialistRecommendationSignal ClassifySignal(SpecialistPerformanceLevel level, SpecialistStatus status)
    {
        if (status == SpecialistStatus.Inactive)
        {
            return SpecialistRecommendationSignal.Attention;
        }

        if (status == SpecialistStatus.OnLeave)
        {
            return SpecialistRecommendationSignal.Monitor;
        }

        return level switch
        {
            SpecialistPerformanceLevel.Excellent => SpecialistRecommendationSignal.Promote,
            SpecialistPerformanceLevel.Good => SpecialistRecommendationSignal.Maintain,
            SpecialistPerformanceLevel.NeedsImprovement => SpecialistRecommendationSignal.Monitor,
            _ => SpecialistRecommendationSignal.Attention,
        };
    }
}
