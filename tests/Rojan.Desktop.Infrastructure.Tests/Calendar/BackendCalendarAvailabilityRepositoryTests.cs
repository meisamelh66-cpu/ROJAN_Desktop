using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Infrastructure.Calendar;

namespace Rojan.Desktop.Infrastructure.Tests.Calendar;

/// <summary>
/// Exercises <see cref="BackendCalendarAvailabilityRepository"/> - the
/// active-specialist filter, the available-slots-&gt;Available-only mapping
/// (see that class's own doc comment for why Booked/Unavailable are never
/// produced), the derived WorkingStart/WorkingEnd, the exact
/// serviceId/date query string sent, and the week view's 7 per-day calls.
/// Only the HTTP transport (<see cref="IApiClient"/>) is faked - same
/// "exercise the real workflow" convention as <c>BackendSpecialistRepositoryTests</c>.
/// </summary>
public sealed class BackendCalendarAvailabilityRepositoryTests
{
    private const string SalonId = "salon-1";
    private static readonly DateOnly TestDate = new(2026, 3, 2);

    [Fact]
    public async Task GetScheduledSpecialistsAsync_ReturnsActiveSpecialistsOnlyOrderedByName()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists"] = new List<SpecialistResponse>
        {
            new("specialist-2", SalonId, null, "Priya Nair", null, null, true),
            new("specialist-3", SalonId, null, "Inactive Person", null, null, false),
            new("specialist-1", SalonId, null, "Jordan Lee", null, null, true),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var specialists = await repository.GetScheduledSpecialistsAsync();

        Assert.Equal(["Jordan Lee", "Priya Nair"], specialists.Select(s => s.Name));
    }

    [Fact]
    public async Task GetScheduledSpecialistsAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetScheduledSpecialistsAsync());
    }

    [Fact]
    public async Task GetScheduledSpecialistsAsync_FetchFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetScheduledSpecialistsAsync());
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_MapsEverySlotAsAvailable_AndDerivesWorkingHoursFromFirstAndLastSlot()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1/available-slots?serviceId=service-1&date=2026-03-02"] =
            new List<TimeSlotResponse>
            {
                new(new DateTime(2026, 3, 2, 9, 0, 0), new DateTime(2026, 3, 2, 9, 30, 0)),
                new(new DateTime(2026, 3, 2, 9, 30, 0), new DateTime(2026, 3, 2, 10, 0, 0)),
            };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "Jordan Lee", null, null, true);

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Equal("Jordan Lee", result.SpecialistName);
        Assert.Equal(2, result.Slots.Count);
        Assert.All(result.Slots, slot => Assert.Equal(AvailabilityStatus.Available, slot.Status));
        Assert.Equal(new TimeSpan(9, 0, 0), result.WorkingStart);
        Assert.Equal(new TimeSpan(10, 0, 0), result.WorkingEnd);
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_NoSlots_NullWorkingHoursAndEmptySlots()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1/available-slots?serviceId=service-1&date=2026-03-02"] =
            new List<TimeSlotResponse>();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "Jordan Lee", null, null, true);

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Empty(result.Slots);
        Assert.Null(result.WorkingStart);
        Assert.Null(result.WorkingEnd);
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_SpecialistNameLookupFails_FallsBackToRawId()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1/available-slots?serviceId=service-1&date=2026-03-02"] =
            new List<TimeSlotResponse>();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Equal("specialist-1", result.SpecialistName);
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_BackendFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/specialist-1/available-slots?serviceId=service-1&date=2026-03-02"] =
            (404, "Specialist, service, or salon not found");
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "Jordan Lee", null, null, true);

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate));
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_CallsAvailableSlotsOnceForEachOfSevenDays()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "Jordan Lee", null, null, true);
        for (var offset = 0; offset < 7; offset++)
        {
            var date = TestDate.AddDays(offset);
            apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1/available-slots?serviceId=service-1&date={date:yyyy-MM-dd}"] =
                new List<TimeSlotResponse>();
        }

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetWeeklyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Equal("specialist-1", result.SpecialistId);
        Assert.Equal("Jordan Lee", result.SpecialistName);
        Assert.Equal(7, result.Days.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(offset => TestDate.AddDays(offset)), result.Days.Select(day => day.Date));
    }

    private static BackendCalendarAvailabilityRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            if (GetFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (GetResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected GET '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCalendarAvailabilityRepository never fetches raw bytes.");
    }
}
