using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Application.Schedule;
using Contracts = Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Infrastructure.Schedule;

/// <summary>
/// Phase 5 Shift Engine: the real, backend-connected <see cref="IScheduleRepository"/> -
/// ROJAN_Backend's <c>SpecialistScheduleController</c>
/// (<c>/api/v1/salons/{salonId}/specialists/{specialistId}/schedule</c>) is
/// the sole authority for weekly availability, one-off overrides, leave,
/// and ad-hoc blocks. No Fake implementation ever existed for this module -
/// unlike every earlier vertical slice in this app, this was built real
/// from day one against an already-verified real contract (see
/// <c>BackendBranchRepository</c>'s own doc comment for the equivalent
/// "why real from day one" reasoning on the Organization/Branch slice).
///
/// Mutating endpoints resolve the current salon via
/// <see cref="ISalonContextService.GetSalonIdAsync"/>, same pattern as
/// every other Backend*Repository in this app - a null resolution throws
/// <see cref="ApiException"/>, since there is no Demo Mode consumer for
/// this module (unlike <c>BackendBranchRepository</c>'s documented
/// fallback) to justify a soft path.
/// </summary>
public sealed class BackendScheduleRepository(IApiClient apiClient, ISalonContextService salonContextService) : IScheduleRepository
{
    public async Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<Contracts.WeeklyAvailabilityResponse>>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/weekly-availability", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load weekly availability for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapWeeklyAvailability).ToList();
    }

    public async Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new Contracts.SetWeeklyAvailabilityRequest(intervals.Select(MapInterval).ToList());
        var response = await apiClient
            .PutAsync<Contracts.SetWeeklyAvailabilityRequest, Contracts.WeeklyAvailabilityResponse>(
                $"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/weekly-availability/{ToBackendDayOfWeek(dayOfWeek)}", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to set weekly availability for specialist '{specialistId}' on {dayOfWeek} (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapWeeklyAvailability(response.Data);
    }

    public async Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/weekly-availability/{ToBackendDayOfWeek(dayOfWeek)}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to clear weekly availability for specialist '{specialistId}' on {dayOfWeek} (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<Contracts.ScheduleOverrideResponse>>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/overrides", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load schedule overrides for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapOverride).ToList();
    }

    public async Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new Contracts.SetScheduleOverrideRequest(intervals.Select(MapInterval).ToList(), reason);
        var response = await apiClient
            .PutAsync<Contracts.SetScheduleOverrideRequest, Contracts.ScheduleOverrideResponse>(
                $"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/overrides/{scheduleDate:yyyy-MM-dd}", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to set schedule override for specialist '{specialistId}' on {scheduleDate:yyyy-MM-dd} (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapOverride(response.Data);
    }

    public async Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/overrides/{overrideId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove schedule override '{overrideId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<Contracts.LeaveResponse>>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/leaves", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load leave records for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapLeave).ToList();
    }

    public async Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new Contracts.CreateLeaveRequest(startDate, endDate, reason);
        var response = await apiClient
            .PostAsync<Contracts.CreateLeaveRequest, Contracts.LeaveResponse>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/leaves", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create leave record for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapLeave(response.Data);
    }

    public async Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/leaves/{leaveId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove leave record '{leaveId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<Contracts.BlockResponse>>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/blocks", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load blocks for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapBlock).ToList();
    }

    public async Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new Contracts.CreateBlockRequest(scheduleDate, start, endTime, reason);
        var response = await apiClient
            .PostAsync<Contracts.CreateBlockRequest, Contracts.BlockResponse>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/blocks", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create block for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapBlock(response.Data);
    }

    public async Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/blocks/{blockId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove block '{blockId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to load a schedule for.");
    }

    /// <summary>ROJAN_Backend binds <c>dayOfWeek</c> as a real Java <c>DayOfWeek</c> enum name (e.g. <c>"MONDAY"</c>) - .NET's own <see cref="DayOfWeek"/> member names match exactly except for casing.</summary>
    private static string ToBackendDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek.ToString().ToUpperInvariant();

    private static Contracts.TimeIntervalDto MapInterval(TimeIntervalDto interval) => new(interval.Start, interval.End);

    private static TimeIntervalDto MapInterval(Contracts.TimeIntervalDto interval) => new(interval.Start, interval.End);

    private static WeeklyAvailabilityDto MapWeeklyAvailability(Contracts.WeeklyAvailabilityResponse response) => new(
        response.Id,
        response.SpecialistId,
        Enum.Parse<DayOfWeek>(response.DayOfWeek, ignoreCase: true),
        response.Intervals.Select(MapInterval).ToList(),
        response.CreatedAt,
        response.UpdatedAt);

    private static ScheduleOverrideDto MapOverride(Contracts.ScheduleOverrideResponse response) => new(
        response.Id, response.SpecialistId, response.Date, response.Intervals.Select(MapInterval).ToList(), response.Reason, response.CreatedAt, response.UpdatedAt);

    private static SpecialistLeaveDto MapLeave(Contracts.LeaveResponse response) => new(
        response.Id, response.SpecialistId, response.StartDate, response.EndDate, response.Reason, response.CreatedAt);

    private static SpecialistBlockDto MapBlock(Contracts.BlockResponse response) => new(
        response.Id, response.SpecialistId, response.Date, response.Start, response.End, response.Reason, response.CreatedAt);
}
