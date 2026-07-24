using AppBookings = Rojan.Desktop.Application.Bookings;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;
using DomainServices = Rojan.Desktop.Domain.Services;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Intelligence;

/// <summary>Default <see cref="IIntelligenceEngine"/> implementation - see the interface's own doc comment for the composition shape this follows.</summary>
public sealed class IntelligenceEngine : IIntelligenceEngine
{
    private readonly AppSpecialists.ISpecialistQueryService _specialistQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;

    public IntelligenceEngine(
        AppSpecialists.ISpecialistQueryService specialistQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        AppBookings.IBookingQueryService bookingQueryService)
    {
        _specialistQueryService = specialistQueryService;
        _serviceQueryService = serviceQueryService;
        _bookingQueryService = bookingQueryService;
    }

    public async Task<IReadOnlyList<SpecialistIntelligenceDto>> GetSpecialistIntelligenceAsync(CancellationToken cancellationToken = default)
    {
        var specialists = await _specialistQueryService.GetSpecialistsAsync(cancellationToken).ConfigureAwait(false);
        if (specialists.Count == 0)
        {
            return [];
        }

        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);

        return specialists
            .Select(specialist => BuildSpecialistIntelligence(specialist, bookings))
            .OrderByDescending(dto => dto.PerformanceScore)
            .ThenBy(dto => dto.SpecialistName, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceIntelligenceDto>> GetServiceIntelligenceAsync(CancellationToken cancellationToken = default)
    {
        var services = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(false);
        if (services.Count == 0)
        {
            return [];
        }

        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.Now;

        return services
            .Select(service => BuildServiceIntelligence(service, bookings, now))
            .OrderByDescending(dto => dto.PopularityScore)
            .ThenBy(dto => dto.ServiceName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Counts this specialist's own bookings by outcome and hands them to <see cref="DomainSpecialists.SpecialistPerformanceCalculator"/> - a specialist with no bookings at all still gets a full result (score 0, <see cref="DomainSpecialists.SpecialistPerformanceLevel.Underperforming"/>), never a failure.</summary>
    private static SpecialistIntelligenceDto BuildSpecialistIntelligence(AppSpecialists.SpecialistDto specialist, IReadOnlyList<AppBookings.BookingDto> bookings)
    {
        var specialistBookings = bookings.Where(booking => booking.SpecialistId == specialist.Id).ToList();
        var completed = specialistBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Completed);
        var cancelled = specialistBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Cancelled);
        var noShow = specialistBookings.Count(booking => booking.Status == AppBookings.BookingStatus.NoShow);

        var indicators = new DomainSpecialists.SpecialistPerformanceIndicators(completed, cancelled, noShow);
        var score = DomainSpecialists.SpecialistPerformanceCalculator.ComputePerformanceScore(indicators);
        var level = DomainSpecialists.SpecialistPerformanceCalculator.ClassifyPerformance(score);
        var domainStatus = AppSpecialists.SpecialistMapper.MapStatusToDomain(specialist.Status);
        var signal = DomainSpecialists.SpecialistPerformanceCalculator.ClassifySignal(level, domainStatus);

        return new SpecialistIntelligenceDto(
            specialist.Id,
            specialist.FullName,
            score,
            IntelligenceMapper.MapPerformanceLevel(level),
            IntelligenceMapper.MapRecommendationSignal(signal),
            completed,
            cancelled,
            noShow);
    }

    /// <summary>
    /// Counts this service's own bookings: completed (proven demand) plus
    /// upcoming (booked, not yet happened, and still in an active
    /// pending/confirmed/in-progress status - a future booking that was
    /// already cancelled or marked no-show is not real demand) and hands
    /// them to <see cref="DomainServices.ServicePopularityCalculator"/>. A
    /// service with no bookings at all still gets a full result (score 0,
    /// <see cref="DomainServices.ServicePopularityLevel.LowDemand"/>), never
    /// a failure.
    /// </summary>
    private static ServiceIntelligenceDto BuildServiceIntelligence(AppServices.ServiceDto service, IReadOnlyList<AppBookings.BookingDto> bookings, DateTimeOffset now)
    {
        var serviceBookings = bookings.Where(booking => booking.ServiceId == service.Id).ToList();
        var completed = serviceBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Completed);
        var upcoming = serviceBookings.Count(booking =>
            booking.ScheduledAt >= now &&
            booking.Status is AppBookings.BookingStatus.Pending or AppBookings.BookingStatus.Confirmed or AppBookings.BookingStatus.InProgress);

        var indicators = new DomainServices.ServicePopularityIndicators(completed, upcoming);
        var score = DomainServices.ServicePopularityCalculator.ComputePopularityScore(indicators);
        var level = DomainServices.ServicePopularityCalculator.ClassifyPopularity(score);
        var domainStatus = AppServices.ServiceMapper.MapStatusToDomain(service.Status);
        var signal = DomainServices.ServicePopularityCalculator.ClassifySignal(level, domainStatus);

        return new ServiceIntelligenceDto(
            service.Id,
            service.Name,
            score,
            IntelligenceMapper.MapPopularityLevel(level),
            IntelligenceMapper.MapRecommendationSignal(signal),
            completed,
            upcoming);
    }
}
