using Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Domain.Tests.Specialists;

public sealed class SpecialistPerformanceCalculatorTests
{
    [Fact]
    public void ComputePerformanceScore_EmptyIndicators_ReturnsZero()
    {
        var indicators = new SpecialistPerformanceIndicators(0, 0, 0);

        Assert.Equal(0, SpecialistPerformanceCalculator.ComputePerformanceScore(indicators));
    }

    [Theory]
    [InlineData(10, 0, 0, 100)]
    [InlineData(5, 0, 0, 50)]
    [InlineData(2, 0, 0, 20)]
    [InlineData(1, 1, 0, 5)]
    [InlineData(1, 0, 1, 0)]
    [InlineData(3, 1, 0, 25)]
    public void ComputePerformanceScore_VariousCounts_MatchesExpectedFormula(
        int completed, int cancelled, int noShow, int expectedScore)
    {
        var indicators = new SpecialistPerformanceIndicators(completed, cancelled, noShow);

        Assert.Equal(expectedScore, SpecialistPerformanceCalculator.ComputePerformanceScore(indicators));
    }

    [Fact]
    public void ComputePerformanceScore_ManyCompletedBookings_IsClampedToOneHundred()
    {
        // 50 completed bookings would raw-score to 500 - the ceiling must hold regardless of tenure.
        var indicators = new SpecialistPerformanceIndicators(50, 0, 0);

        Assert.Equal(100, SpecialistPerformanceCalculator.ComputePerformanceScore(indicators));
    }

    [Fact]
    public void ComputePerformanceScore_PenaltiesExceedEarnedPoints_IsClampedToZeroNotNegative()
    {
        var indicators = new SpecialistPerformanceIndicators(1, 0, 5);

        Assert.Equal(0, SpecialistPerformanceCalculator.ComputePerformanceScore(indicators));
    }

    [Theory]
    [InlineData(-10, 0, 0)]
    [InlineData(0, -5, 0)]
    [InlineData(0, 0, -5)]
    [InlineData(-10, -10, -10)]
    public void ComputePerformanceScore_NegativeCounts_AreTreatedAsZeroNeverThrows(int completed, int cancelled, int noShow)
    {
        // Invalid/degenerate input: negative counts should never happen in practice, but the
        // calculator must degrade gracefully (treat as zero) rather than throw or return a
        // nonsensical negative score.
        var indicators = new SpecialistPerformanceIndicators(completed, cancelled, noShow);

        var score = SpecialistPerformanceCalculator.ComputePerformanceScore(indicators);

        Assert.Equal(0, score);
    }

    [Theory]
    [InlineData(100, SpecialistPerformanceLevel.Excellent)]
    [InlineData(80, SpecialistPerformanceLevel.Excellent)]
    [InlineData(79, SpecialistPerformanceLevel.Good)]
    [InlineData(50, SpecialistPerformanceLevel.Good)]
    [InlineData(49, SpecialistPerformanceLevel.NeedsImprovement)]
    [InlineData(20, SpecialistPerformanceLevel.NeedsImprovement)]
    [InlineData(19, SpecialistPerformanceLevel.Underperforming)]
    [InlineData(0, SpecialistPerformanceLevel.Underperforming)]
    public void ClassifyPerformance_VariousScores_MatchesExpectedBoundaries(int score, SpecialistPerformanceLevel expected)
    {
        Assert.Equal(expected, SpecialistPerformanceCalculator.ClassifyPerformance(score));
    }

    [Theory]
    [InlineData(SpecialistPerformanceLevel.Excellent, SpecialistStatus.Active, SpecialistRecommendationSignal.Promote)]
    [InlineData(SpecialistPerformanceLevel.Good, SpecialistStatus.Active, SpecialistRecommendationSignal.Maintain)]
    [InlineData(SpecialistPerformanceLevel.NeedsImprovement, SpecialistStatus.Active, SpecialistRecommendationSignal.Monitor)]
    [InlineData(SpecialistPerformanceLevel.Underperforming, SpecialistStatus.Active, SpecialistRecommendationSignal.Attention)]
    public void ClassifySignal_ActiveSpecialist_SignalDrivenPurelyByLevel(
        SpecialistPerformanceLevel level, SpecialistStatus status, SpecialistRecommendationSignal expected)
    {
        Assert.Equal(expected, SpecialistPerformanceCalculator.ClassifySignal(level, status));
    }

    [Theory]
    [InlineData(SpecialistPerformanceLevel.Excellent)]
    [InlineData(SpecialistPerformanceLevel.Good)]
    [InlineData(SpecialistPerformanceLevel.NeedsImprovement)]
    [InlineData(SpecialistPerformanceLevel.Underperforming)]
    public void ClassifySignal_InactiveSpecialist_AlwaysNeedsAttentionRegardlessOfLevel(SpecialistPerformanceLevel level)
    {
        // Invalid/edge combination: even an Excellent past performance level cannot override an
        // Inactive (archived) specialist needing Attention.
        Assert.Equal(SpecialistRecommendationSignal.Attention, SpecialistPerformanceCalculator.ClassifySignal(level, SpecialistStatus.Inactive));
    }

    [Theory]
    [InlineData(SpecialistPerformanceLevel.Excellent)]
    [InlineData(SpecialistPerformanceLevel.Underperforming)]
    public void ClassifySignal_OnLeaveSpecialist_AlwaysMonitorRegardlessOfLevel(SpecialistPerformanceLevel level)
    {
        Assert.Equal(SpecialistRecommendationSignal.Monitor, SpecialistPerformanceCalculator.ClassifySignal(level, SpecialistStatus.OnLeave));
    }
}
