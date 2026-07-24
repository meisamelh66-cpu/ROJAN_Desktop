using Rojan.Desktop.Application.Intelligence;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.Intelligence;

public sealed class IntelligenceEngineTests
{
    private static AppSpecialists.SpecialistDto MakeSpecialist(string id, string fullName, AppSpecialists.SpecialistStatus status = AppSpecialists.SpecialistStatus.Active) =>
        new(id, fullName, "Stylist", $"{id}@example.com", "+1 555 010 2231", status, "Bio");

    private static AppServices.ServiceDto MakeService(string id, string name, AppServices.ServiceStatus status = AppServices.ServiceStatus.Active) =>
        new(id, name, AppServices.ServiceCategory.Hair, status, 60, "80", "Description");

    private static AppBookings.BookingDto MakeBooking(
        string id, string specialistId, string serviceId, DateTimeOffset scheduledAt, AppBookings.BookingStatus status) =>
        new(id, "customer-1", "Amelia Hart", serviceId, "Haircut", specialistId, "Specialist",
            scheduledAt, 60, "80", status, string.Empty, "org-1", "branch-1");

    // ----- Specialist intelligence -----

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_NoSpecialists_ReturnsEmptyListWithoutThrowing()
    {
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut")]),
            new StubBookingQueryService([]));

        var result = await sut.GetSpecialistIntelligenceAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_SpecialistWithNoBookings_ReturnsZeroScoreAndUnderperforming()
    {
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([MakeSpecialist("specialist-1", "Alex Stylist")]),
            new StubServiceQueryService([]),
            new StubBookingQueryService([]));

        var result = await sut.GetSpecialistIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(0, intelligence.PerformanceScore);
        Assert.Equal(SpecialistPerformanceLevel.Underperforming, intelligence.PerformanceLevel);
        Assert.Equal(0, intelligence.CompletedBookingCount);
        Assert.Equal(0, intelligence.CancelledBookingCount);
        Assert.Equal(0, intelligence.NoShowBookingCount);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_NormalMixedBookings_ComputesCorrectCountsAndScore()
    {
        var now = DateTimeOffset.Now;
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-1", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-1", "service-1", now.AddDays(-4), AppBookings.BookingStatus.Completed),
            MakeBooking("b5", "specialist-1", "service-1", now.AddDays(-5), AppBookings.BookingStatus.Completed),
            MakeBooking("b6", "specialist-1", "service-1", now.AddDays(-6), AppBookings.BookingStatus.Completed),
            MakeBooking("b7", "specialist-1", "service-1", now.AddDays(-7), AppBookings.BookingStatus.Cancelled),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([MakeSpecialist("specialist-1", "Alex Stylist")]),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(6, intelligence.CompletedBookingCount);
        Assert.Equal(1, intelligence.CancelledBookingCount);
        Assert.Equal(0, intelligence.NoShowBookingCount);
        Assert.Equal(55, intelligence.PerformanceScore); // (6 * 10) - (1 * 5) = 55
        Assert.Equal(SpecialistPerformanceLevel.Good, intelligence.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Maintain, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_EightCompletedBookings_CrossesExcellentBoundary()
    {
        var now = DateTimeOffset.Now;
        var bookings = Enumerable.Range(0, 8)
            .Select(i => MakeBooking($"b{i}", "specialist-1", "service-1", now.AddDays(-i - 1), AppBookings.BookingStatus.Completed))
            .ToList();
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([MakeSpecialist("specialist-1", "Alex Stylist")]),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(80, intelligence.PerformanceScore);
        Assert.Equal(SpecialistPerformanceLevel.Excellent, intelligence.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Promote, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_MultipleSpecialists_OrderedByScoreDescending()
    {
        var now = DateTimeOffset.Now;
        var specialists = new List<AppSpecialists.SpecialistDto>
        {
            MakeSpecialist("specialist-low", "Low Performer"),
            MakeSpecialist("specialist-high", "High Performer"),
            MakeSpecialist("specialist-mid", "Mid Performer"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-high", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-high", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-high", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-high", "service-1", now.AddDays(-4), AppBookings.BookingStatus.Completed),
            MakeBooking("b5", "specialist-high", "service-1", now.AddDays(-5), AppBookings.BookingStatus.Completed),
            MakeBooking("b6", "specialist-high", "service-1", now.AddDays(-6), AppBookings.BookingStatus.Completed),
            MakeBooking("b7", "specialist-high", "service-1", now.AddDays(-7), AppBookings.BookingStatus.Completed),
            MakeBooking("b8", "specialist-high", "service-1", now.AddDays(-8), AppBookings.BookingStatus.Completed),
            MakeBooking("b9", "specialist-mid", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b10", "specialist-mid", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b11", "specialist-mid", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b12", "specialist-low", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService(specialists),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        Assert.Equal(["specialist-high", "specialist-mid", "specialist-low"], result.Select(r => r.SpecialistId));
        Assert.True(result[0].PerformanceScore >= result[1].PerformanceScore);
        Assert.True(result[1].PerformanceScore >= result[2].PerformanceScore);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_TiedScores_TieBrokenByNameOrdinalAscending()
    {
        var now = DateTimeOffset.Now;
        var specialists = new List<AppSpecialists.SpecialistDto>
        {
            MakeSpecialist("specialist-z", "Zara Doe"),
            MakeSpecialist("specialist-a", "Amy Lee"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-z", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-z", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-z", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-a", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b5", "specialist-a", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b6", "specialist-a", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService(specialists),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        Assert.Equal(result[0].PerformanceScore, result[1].PerformanceScore);
        Assert.Equal("Amy Lee", result[0].SpecialistName);
        Assert.Equal("Zara Doe", result[1].SpecialistName);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_BookingReferencesUnknownSpecialistId_IsIgnored()
    {
        var now = DateTimeOffset.Now;
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "ghost-specialist", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([MakeSpecialist("specialist-1", "Alex Stylist")]),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(1, intelligence.CompletedBookingCount);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_InactiveSpecialistWithHighScore_SignalIsAttentionNotPromote()
    {
        var now = DateTimeOffset.Now;
        var bookings = Enumerable.Range(0, 10)
            .Select(i => MakeBooking($"b{i}", "specialist-1", "service-1", now.AddDays(-i - 1), AppBookings.BookingStatus.Completed))
            .ToList();
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([MakeSpecialist("specialist-1", "Alex Stylist", AppSpecialists.SpecialistStatus.Inactive)]),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetSpecialistIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(SpecialistPerformanceLevel.Excellent, intelligence.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Attention, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetSpecialistIntelligenceAsync_CalledTwiceWithSameInput_ReturnsDeterministicIdenticalResults()
    {
        var now = DateTimeOffset.Now;
        var specialists = new List<AppSpecialists.SpecialistDto>
        {
            MakeSpecialist("specialist-1", "Alex Stylist"),
            MakeSpecialist("specialist-2", "Blair Stylist"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-2", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Cancelled),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService(specialists),
            new StubServiceQueryService([]),
            new StubBookingQueryService(bookings));

        var first = await sut.GetSpecialistIntelligenceAsync();
        var second = await sut.GetSpecialistIntelligenceAsync();

        Assert.Equal(first, second);
    }

    // ----- Service intelligence -----

    [Fact]
    public async Task GetServiceIntelligenceAsync_NoServices_ReturnsEmptyListWithoutThrowing()
    {
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([]),
            new StubBookingQueryService([]));

        var result = await sut.GetServiceIntelligenceAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_ServiceWithNoBookings_ReturnsZeroScoreAndLowDemand()
    {
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut")]),
            new StubBookingQueryService([]));

        var result = await sut.GetServiceIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(0, intelligence.PopularityScore);
        Assert.Equal(ServicePopularityLevel.LowDemand, intelligence.PopularityLevel);
        Assert.Equal(0, intelligence.CompletedBookingCount);
        Assert.Equal(0, intelligence.UpcomingBookingCount);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_NormalMixedBookings_ComputesCorrectCountsAndScore()
    {
        var now = DateTimeOffset.Now;
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "service-1", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-1", "service-1", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-1", "service-1", now.AddDays(-4), AppBookings.BookingStatus.Completed),
            MakeBooking("b5", "specialist-1", "service-1", now.AddDays(5), AppBookings.BookingStatus.Confirmed),
            MakeBooking("b6", "specialist-1", "service-1", now.AddDays(6), AppBookings.BookingStatus.Pending),
            MakeBooking("b7", "specialist-1", "service-1", now.AddDays(7), AppBookings.BookingStatus.Cancelled), // future but cancelled: not "upcoming"
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut")]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(4, intelligence.CompletedBookingCount);
        Assert.Equal(2, intelligence.UpcomingBookingCount);
        Assert.Equal(40, intelligence.PopularityScore); // (4 * 8) + (2 * 4) = 40
        Assert.Equal(ServicePopularityLevel.Trending, intelligence.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Maintain, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_NineCompletedBookings_CrossesPopularBoundary()
    {
        var now = DateTimeOffset.Now;
        var bookings = Enumerable.Range(0, 9)
            .Select(i => MakeBooking($"b{i}", "specialist-1", "service-1", now.AddDays(-i - 1), AppBookings.BookingStatus.Completed))
            .ToList();
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut")]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(72, intelligence.PopularityScore);
        Assert.Equal(ServicePopularityLevel.Popular, intelligence.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Feature, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_MultipleServices_OrderedByScoreDescending()
    {
        var now = DateTimeOffset.Now;
        var services = new List<AppServices.ServiceDto>
        {
            MakeService("service-low", "Low Demand Service"),
            MakeService("service-high", "High Demand Service"),
            MakeService("service-mid", "Mid Demand Service"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-high", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "service-high", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-1", "service-high", now.AddDays(-3), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-1", "service-high", now.AddDays(-4), AppBookings.BookingStatus.Completed),
            MakeBooking("b5", "specialist-1", "service-high", now.AddDays(-5), AppBookings.BookingStatus.Completed),
            MakeBooking("b6", "specialist-1", "service-mid", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b7", "specialist-1", "service-mid", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b8", "specialist-1", "service-low", now.AddDays(-1), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService(services),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        Assert.Equal(["service-high", "service-mid", "service-low"], result.Select(r => r.ServiceId));
        Assert.True(result[0].PopularityScore >= result[1].PopularityScore);
        Assert.True(result[1].PopularityScore >= result[2].PopularityScore);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_TiedScores_TieBrokenByNameOrdinalAscending()
    {
        var now = DateTimeOffset.Now;
        var services = new List<AppServices.ServiceDto>
        {
            MakeService("service-z", "Zen Facial"),
            MakeService("service-a", "Aroma Massage"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-z", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "service-z", now.AddDays(-2), AppBookings.BookingStatus.Completed),
            MakeBooking("b3", "specialist-1", "service-a", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b4", "specialist-1", "service-a", now.AddDays(-2), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService(services),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        Assert.Equal(result[0].PopularityScore, result[1].PopularityScore);
        Assert.Equal("Aroma Massage", result[0].ServiceName);
        Assert.Equal("Zen Facial", result[1].ServiceName);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_BookingReferencesUnknownServiceId_IsIgnored()
    {
        var now = DateTimeOffset.Now;
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "ghost-service", now.AddDays(-1), AppBookings.BookingStatus.Completed),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut")]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(1, intelligence.CompletedBookingCount);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_DiscontinuedServiceWithHighScore_SignalIsReconsiderNotFeature()
    {
        var now = DateTimeOffset.Now;
        var bookings = Enumerable.Range(0, 10)
            .Select(i => MakeBooking($"b{i}", "specialist-1", "service-1", now.AddDays(-i - 1), AppBookings.BookingStatus.Completed))
            .ToList();
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService([MakeService("service-1", "Haircut", AppServices.ServiceStatus.Discontinued)]),
            new StubBookingQueryService(bookings));

        var result = await sut.GetServiceIntelligenceAsync();

        var intelligence = Assert.Single(result);
        Assert.Equal(ServicePopularityLevel.Popular, intelligence.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Reconsider, intelligence.RecommendationSignal);
    }

    [Fact]
    public async Task GetServiceIntelligenceAsync_CalledTwiceWithSameInput_ReturnsDeterministicIdenticalResults()
    {
        var now = DateTimeOffset.Now;
        var services = new List<AppServices.ServiceDto>
        {
            MakeService("service-1", "Haircut"),
            MakeService("service-2", "Manicure"),
        };
        var bookings = new List<AppBookings.BookingDto>
        {
            MakeBooking("b1", "specialist-1", "service-1", now.AddDays(-1), AppBookings.BookingStatus.Completed),
            MakeBooking("b2", "specialist-1", "service-2", now.AddDays(3), AppBookings.BookingStatus.Pending),
        };
        var sut = new IntelligenceEngine(
            new StubSpecialistQueryService([]),
            new StubServiceQueryService(services),
            new StubBookingQueryService(bookings));

        var first = await sut.GetServiceIntelligenceAsync();
        var second = await sut.GetServiceIntelligenceAsync();

        Assert.Equal(first, second);
    }
}
