using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Specialists.Schedule;
using Rojan.Desktop.Infrastructure.Specialists.Schedule;

namespace Rojan.Desktop.Infrastructure.Tests.Specialists.Schedule;

/// <summary>
/// Exercises <see cref="BackendSpecialistScheduleRepository"/> against
/// ROJAN_Backend's real <c>SpecialistScheduleController</c> wire shapes -
/// the <see cref="DayOfWeek"/> string-to-enum mapping, ISO-8601 date path
/// segments, upper-case day-of-week path segments (matching Spring's own
/// default enum path-variable converter), the redacted-<c>reason</c>
/// passthrough, and the empty-intervals-is-a-real-state case. Only the
/// HTTP transport (<see cref="IApiClient"/>) is faked - same "exercise the
/// real workflow" convention as <c>BackendSpecialistRepositoryTests</c>.
/// </summary>
public sealed class BackendSpecialistScheduleRepositoryTests
{
    private const string SalonId = "salon-1";
    private const string SpecialistId = "specialist-1";
    private const string SchedulePrefix = $"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule";

    // ---- Weekly availability ----

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_MapsDayOfWeekStringToEnum_CaseInsensitive()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"{SchedulePrefix}/weekly-availability"] = new List<WeeklyAvailabilityResponse>
        {
            new("wa-1", SpecialistId, "MONDAY", [new ScheduleTimeIntervalDto(TimeSpan.FromHours(9), TimeSpan.FromHours(13))]),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var availability = Assert.Single(await repository.GetWeeklyAvailabilityAsync(SpecialistId));

        Assert.Equal(DayOfWeek.Monday, availability.DayOfWeek);
        Assert.Equal(TimeSpan.FromHours(9), availability.Intervals[0].Start);
        Assert.Equal(TimeSpan.FromHours(13), availability.Intervals[0].End);
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_FetchFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"{SchedulePrefix}/weekly-availability"] = (500, "Server error");
        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetWeeklyAvailabilityAsync(SpecialistId));
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetWeeklyAvailabilityAsync(SpecialistId));
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_SendsUpperCaseDaySegment_MatchingSpringsEnumConverter()
    {
        var apiClient = new StubApiClient
        {
            PutResponse = new WeeklyAvailabilityResponse("wa-1", SpecialistId, "TUESDAY", [new ScheduleTimeIntervalDto(TimeSpan.FromHours(10), TimeSpan.FromHours(18))]),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.SetWeeklyAvailabilityAsync(SpecialistId, DayOfWeek.Tuesday, [new TimeInterval(TimeSpan.FromHours(10), TimeSpan.FromHours(18))]);

        Assert.Equal($"{SchedulePrefix}/weekly-availability/TUESDAY", apiClient.LastPutCall?.Path);
        Assert.Equal(DayOfWeek.Tuesday, result.DayOfWeek);
    }

    [Fact]
    public async Task RemoveWeeklyAvailabilityAsync_SendsUpperCaseDaySegment()
    {
        var apiClient = new StubApiClient();
        var repository = CreateRepository(apiClient, SalonId);

        await repository.RemoveWeeklyAvailabilityAsync(SpecialistId, DayOfWeek.Sunday);

        Assert.Equal($"{SchedulePrefix}/weekly-availability/SUNDAY", apiClient.LastDeleteCall);
    }

    [Fact]
    public async Task RemoveWeeklyAvailabilityAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient { DeleteFailure = (403, "Forbidden") };
        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.RemoveWeeklyAvailabilityAsync(SpecialistId, DayOfWeek.Monday));
    }

    // ---- Overrides ----

    [Fact]
    public async Task GetOverridesAsync_EmptyIntervals_IsARealUnavailableAllDayState_NotAnError()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"{SchedulePrefix}/overrides"] = new List<ScheduleOverrideResponse>
        {
            new("ov-1", SpecialistId, new DateOnly(2026, 9, 1), [], "Public holiday"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var @override = Assert.Single(await repository.GetOverridesAsync(SpecialistId));

        Assert.Empty(@override.Intervals);
        Assert.Equal("Public holiday", @override.Reason);
    }

    [Fact]
    public async Task GetOverridesAsync_RedactedReason_PassesThroughAsNull_NotFabricated()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"{SchedulePrefix}/overrides"] = new List<ScheduleOverrideResponse>
        {
            new("ov-1", SpecialistId, new DateOnly(2026, 9, 1), [], Reason: null),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var @override = Assert.Single(await repository.GetOverridesAsync(SpecialistId));

        Assert.Null(@override.Reason);
    }

    [Fact]
    public async Task SetOverrideAsync_SendsIsoDateSegment()
    {
        var apiClient = new StubApiClient
        {
            PutResponse = new ScheduleOverrideResponse("ov-1", SpecialistId, new DateOnly(2026, 9, 1), [], "Holiday"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        await repository.SetOverrideAsync(SpecialistId, new DateOnly(2026, 9, 1), [], "Holiday");

        Assert.Equal($"{SchedulePrefix}/overrides/2026-09-01", apiClient.LastPutCall?.Path);
    }

    [Fact]
    public async Task RemoveOverrideAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient { DeleteFailure = (404, "Not found") };
        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.RemoveOverrideAsync(SpecialistId, "ov-1"));
    }

    // ---- Leave ----

    [Fact]
    public async Task GetLeaveAsync_MapsDateRangeAndReason()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"{SchedulePrefix}/leaves"] = new List<LeaveResponse>
        {
            new("lv-1", SpecialistId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var leave = Assert.Single(await repository.GetLeaveAsync(SpecialistId));

        Assert.Equal(new DateOnly(2026, 9, 1), leave.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 7), leave.EndDate);
        Assert.Equal("Vacation", leave.Reason);
    }

    [Fact]
    public async Task CreateLeaveAsync_PostsToLeavesEndpoint()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new LeaveResponse("lv-1", SpecialistId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var leave = await repository.CreateLeaveAsync(SpecialistId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation");

        Assert.Equal($"{SchedulePrefix}/leaves", apiClient.LastPostCall?.Path);
        Assert.Equal("lv-1", leave.Id);
    }

    [Fact]
    public async Task RemoveLeaveAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient { DeleteFailure = (500, "Server error") };
        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.RemoveLeaveAsync(SpecialistId, "lv-1"));
    }

    // ---- Blocks ----

    [Fact]
    public async Task GetBlocksAsync_MapsIntervalAndReason()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"{SchedulePrefix}/blocks"] = new List<BlockResponse>
        {
            new("bl-1", SpecialistId, new DateOnly(2026, 9, 1), TimeSpan.FromHours(14), TimeSpan.FromHours(15), "Dentist"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var block = Assert.Single(await repository.GetBlocksAsync(SpecialistId));

        Assert.Equal(TimeSpan.FromHours(14), block.Interval.Start);
        Assert.Equal(TimeSpan.FromHours(15), block.Interval.End);
        Assert.Equal("Dentist", block.Reason);
    }

    [Fact]
    public async Task CreateBlockAsync_PostsToBlocksEndpoint()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new BlockResponse("bl-1", SpecialistId, new DateOnly(2026, 9, 1), TimeSpan.FromHours(14), TimeSpan.FromHours(15), "Dentist"),
        };
        var repository = CreateRepository(apiClient, SalonId);

        var block = await repository.CreateBlockAsync(SpecialistId, new DateOnly(2026, 9, 1), new TimeInterval(TimeSpan.FromHours(14), TimeSpan.FromHours(15)), "Dentist");

        Assert.Equal($"{SchedulePrefix}/blocks", apiClient.LastPostCall?.Path);
        Assert.Equal("bl-1", block.Id);
    }

    [Fact]
    public async Task RemoveBlockAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient { DeleteFailure = (500, "Server error") };
        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.RemoveBlockAsync(SpecialistId, "bl-1"));
    }

    private static BackendSpecialistScheduleRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public object? PostResponse { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public object? PutResponse { get; set; }

        public (string Path, object? Body)? LastPutCall { get; private set; }

        public (int? Status, string Message)? DeleteFailure { get; set; }

        public string? LastDeleteCall { get; private set; }

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

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPostCall = (path, body);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PostResponse!, 201));
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPutCall = (path, body);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PutResponse!, 200));
        }

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            LastDeleteCall = path;

            if (DeleteFailure is { } failure)
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            return Task.FromResult(ApiResponseFactory.Success(default(TResponse)!, 204));
        }

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistScheduleRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistScheduleRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistScheduleRepository never fetches raw bytes.");
    }
}
