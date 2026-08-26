using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Application.Schedule;
using Rojan.Desktop.Infrastructure.Schedule;

namespace Rojan.Desktop.Infrastructure.Tests.Schedule;

/// <summary>
/// Exercises <see cref="BackendScheduleRepository"/> - real endpoint paths against
/// ROJAN_Backend's <c>SpecialistScheduleController</c>, the DayOfWeek path-segment
/// mapping, the redacted-reason pass-through, and that a real backend failure throws
/// rather than falling back to anything local (no Demo Mode consumer exists for this
/// module, unlike <c>BackendBranchRepositoryTests</c>). Only the HTTP transport
/// (<see cref="IApiClient"/>) is faked - same convention as every other
/// Backend*RepositoryTests in this app.
/// </summary>
public sealed class BackendScheduleRepositoryTests
{
    private const string SalonId = "salon-1";
    private const string SpecialistId = "specialist-1";

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_MapsRealFields()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/weekly-availability"] = new List<WeeklyAvailabilityResponse>
        {
            new("avail-1", SpecialistId, "MONDAY", [new Rojan.Desktop.Application.Api.Contracts.TimeIntervalDto(new TimeOnly(9, 0), new TimeOnly(17, 0))], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetWeeklyAvailabilityAsync(SpecialistId);

        var entry = Assert.Single(result);
        Assert.Equal(DayOfWeek.Monday, entry.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), entry.Intervals[0].Start);
        Assert.Equal(new TimeOnly(17, 0), entry.Intervals[0].End);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_BuildsUppercaseDayOfWeekPathSegment()
    {
        var apiClient = new StubApiClient();
        apiClient.PutResponses[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/weekly-availability/TUESDAY"] =
            new WeeklyAvailabilityResponse("avail-1", SpecialistId, "TUESDAY", [new Rojan.Desktop.Application.Api.Contracts.TimeIntervalDto(new TimeOnly(10, 0), new TimeOnly(18, 0))], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.SetWeeklyAvailabilityAsync(SpecialistId, DayOfWeek.Tuesday, [new Rojan.Desktop.Application.Schedule.TimeIntervalDto(new TimeOnly(10, 0), new TimeOnly(18, 0))]);

        Assert.Equal(DayOfWeek.Tuesday, result.DayOfWeek);
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_NoRealSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetWeeklyAvailabilityAsync(SpecialistId));
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_RequestFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/weekly-availability"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetWeeklyAvailabilityAsync(SpecialistId));
    }

    [Fact]
    public async Task GetOverridesAsync_PassesThroughRedactedReasonUnmodified()
    {
        // Backend itself decides redaction (null Reason for a non-owner viewer) - this repository
        // must never re-derive or override that decision.
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/overrides"] = new List<ScheduleOverrideResponse>
        {
            new("override-1", SpecialistId, new DateOnly(2026, 6, 1), [], null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.GetOverridesAsync(SpecialistId);

        Assert.Null(Assert.Single(result).Reason);
    }

    [Fact]
    public async Task CreateLeaveAsync_SendsRealFieldsAndMapsResponse()
    {
        var apiClient = new StubApiClient();
        apiClient.PostResponses[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/leaves"] =
            new LeaveResponse("leave-1", SpecialistId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "Vacation", DateTimeOffset.UtcNow);

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.CreateLeaveAsync(SpecialistId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "Vacation");

        Assert.Equal("leave-1", result.Id);
        var sentRequest = Assert.IsType<CreateLeaveRequest>(apiClient.LastPostBody);
        Assert.Equal("Vacation", sentRequest.Reason);
    }

    [Fact]
    public async Task CreateBlockAsync_SendsRealFieldsAndMapsResponse()
    {
        var apiClient = new StubApiClient();
        apiClient.PostResponses[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/blocks"] =
            new BlockResponse("block-1", SpecialistId, new DateOnly(2026, 6, 10), new TimeOnly(13, 0), new TimeOnly(14, 0), "Dentist", DateTimeOffset.UtcNow);

        var repository = CreateRepository(apiClient, SalonId);

        var result = await repository.CreateBlockAsync(SpecialistId, new DateOnly(2026, 6, 10), new TimeOnly(13, 0), new TimeOnly(14, 0), "Dentist");

        Assert.Equal(new TimeOnly(13, 0), result.Start);
        Assert.Equal(new TimeOnly(14, 0), result.End);
    }

    [Fact]
    public async Task RemoveBlockAsync_RequestFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.DeleteFailures[$"/api/v1/salons/{SalonId}/specialists/{SpecialistId}/schedule/blocks/block-1"] = (403, "Forbidden");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.RemoveBlockAsync(SpecialistId, "block-1"));
    }

    private static BackendScheduleRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Dictionary<string, object> PostResponses { get; } = [];

        public Dictionary<string, object> PutResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> DeleteFailures { get; } = [];

        public object? LastPostBody { get; private set; }

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
            LastPostBody = body;

            if (PostResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 201));
            }

            throw new InvalidOperationException($"Unexpected POST '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            if (PutResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected PUT '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            if (DeleteFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            return Task.FromResult(ApiResponseFactory.Success(default(TResponse)!, 204));
        }

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendScheduleRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendScheduleRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendScheduleRepository never fetches raw bytes.");
    }
}
