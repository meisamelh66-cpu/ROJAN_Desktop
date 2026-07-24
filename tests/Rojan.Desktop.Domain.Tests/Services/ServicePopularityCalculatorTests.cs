using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Domain.Tests.Services;

public sealed class ServicePopularityCalculatorTests
{
    [Fact]
    public void ComputePopularityScore_EmptyIndicators_ReturnsZero()
    {
        var indicators = new ServicePopularityIndicators(0, 0);

        Assert.Equal(0, ServicePopularityCalculator.ComputePopularityScore(indicators));
    }

    [Theory]
    [InlineData(10, 0, 80)]
    [InlineData(0, 10, 40)]
    [InlineData(5, 5, 60)]
    [InlineData(1, 0, 8)]
    [InlineData(0, 1, 4)]
    public void ComputePopularityScore_VariousCounts_MatchesExpectedFormula(
        int completed, int upcoming, int expectedScore)
    {
        var indicators = new ServicePopularityIndicators(completed, upcoming);

        Assert.Equal(expectedScore, ServicePopularityCalculator.ComputePopularityScore(indicators));
    }

    [Fact]
    public void ComputePopularityScore_ManyBookings_IsClampedToOneHundred()
    {
        // 50 completed bookings would raw-score to 400 - the ceiling must hold regardless of catalog age.
        var indicators = new ServicePopularityIndicators(50, 50);

        Assert.Equal(100, ServicePopularityCalculator.ComputePopularityScore(indicators));
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, -10)]
    [InlineData(-5, -5)]
    public void ComputePopularityScore_NegativeCounts_AreTreatedAsZeroNeverThrows(int completed, int upcoming)
    {
        // Invalid/degenerate input: negative counts should never happen in practice, but the
        // calculator must degrade gracefully (treat as zero) rather than throw or return a
        // nonsensical negative score.
        var indicators = new ServicePopularityIndicators(completed, upcoming);

        var score = ServicePopularityCalculator.ComputePopularityScore(indicators);

        Assert.Equal(0, score);
    }

    [Theory]
    [InlineData(100, ServicePopularityLevel.Popular)]
    [InlineData(70, ServicePopularityLevel.Popular)]
    [InlineData(69, ServicePopularityLevel.Trending)]
    [InlineData(35, ServicePopularityLevel.Trending)]
    [InlineData(34, ServicePopularityLevel.Standard)]
    [InlineData(10, ServicePopularityLevel.Standard)]
    [InlineData(9, ServicePopularityLevel.LowDemand)]
    [InlineData(0, ServicePopularityLevel.LowDemand)]
    public void ClassifyPopularity_VariousScores_MatchesExpectedBoundaries(int score, ServicePopularityLevel expected)
    {
        Assert.Equal(expected, ServicePopularityCalculator.ClassifyPopularity(score));
    }

    [Theory]
    [InlineData(ServicePopularityLevel.Popular, ServiceStatus.Active, ServiceRecommendationSignal.Feature)]
    [InlineData(ServicePopularityLevel.Trending, ServiceStatus.Active, ServiceRecommendationSignal.Maintain)]
    [InlineData(ServicePopularityLevel.Standard, ServiceStatus.Active, ServiceRecommendationSignal.Monitor)]
    [InlineData(ServicePopularityLevel.LowDemand, ServiceStatus.Active, ServiceRecommendationSignal.Reconsider)]
    public void ClassifySignal_ActiveService_SignalDrivenPurelyByLevel(
        ServicePopularityLevel level, ServiceStatus status, ServiceRecommendationSignal expected)
    {
        Assert.Equal(expected, ServicePopularityCalculator.ClassifySignal(level, status));
    }

    [Theory]
    [InlineData(ServicePopularityLevel.Popular)]
    [InlineData(ServicePopularityLevel.Trending)]
    [InlineData(ServicePopularityLevel.Standard)]
    [InlineData(ServicePopularityLevel.LowDemand)]
    public void ClassifySignal_DiscontinuedService_AlwaysReconsiderRegardlessOfLevel(ServicePopularityLevel level)
    {
        // Invalid/edge combination: even a Popular past-demand level cannot override a
        // Discontinued service needing Reconsideration.
        Assert.Equal(ServiceRecommendationSignal.Reconsider, ServicePopularityCalculator.ClassifySignal(level, ServiceStatus.Discontinued));
    }

    [Theory]
    [InlineData(ServicePopularityLevel.Popular)]
    [InlineData(ServicePopularityLevel.LowDemand)]
    public void ClassifySignal_SeasonalService_AlwaysMonitorRegardlessOfLevel(ServicePopularityLevel level)
    {
        Assert.Equal(ServiceRecommendationSignal.Monitor, ServicePopularityCalculator.ClassifySignal(level, ServiceStatus.Seasonal));
    }
}
