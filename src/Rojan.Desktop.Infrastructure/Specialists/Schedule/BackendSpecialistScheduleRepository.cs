using System.Globalization;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Infrastructure.Specialists.Schedule;

/// <summary>
/// Phase 7.2.4 Shift Engine (Specialist Schedule) Backend Integration - the
/// real, backend-connected <see cref="DomainSchedule.ISpecialistScheduleRepository"/>,
/// consuming ROJAN_Backend's <c>SpecialistScheduleController</c> directly
/// (Architecture Decision v1, Option A). No Fake predecessor to retain
/// unreferenced - same "genuinely new vertical slice" shape as
/// <c>Salons.BackendSalonRepository</c>, not a Fake/Ef-&gt;Backend swap.
///
/// Honesty notes, all deliberate:
/// <list type="bullet">
/// <item><see cref="WeeklyAvailabilityResponse.DayOfWeek"/> arrives as a
/// raw string (e.g. <c>"MONDAY"</c>) - mapped explicitly via
/// <see cref="Enum.Parse{TEnum}(string, bool)"/>, never trusting automatic
/// enum deserialization. See <c>Api.Contracts.SpecialistScheduleContracts</c>'s
/// own doc comment for why.</item>
/// <item>The <c>{dayOfWeek}</c>/<c>{date}</c> path segments are formatted
/// to match Spring's own default path-variable converters exactly -
/// upper-case enum name (<c>"MONDAY"</c>, matching Java's
/// <c>DayOfWeek.valueOf</c>, case-sensitive) and ISO-8601
/// <c>yyyy-MM-dd</c> respectively.</item>
/// <item>No conflict validation, no permission check anywhere in this
/// class - see <see cref="DomainSchedule.ISpecialistScheduleRepository"/>'s
/// own doc comment for why both are deliberately out of scope here.</item>
/// </list>
/// </summary>
public sealed class BackendSpecialistScheduleRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService) : DomainSchedule.ISpecialistScheduleRepository
{
    public async Task<IReadOnlyList<DomainSchedule.WeeklyAvailability>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<WeeklyAvailabilityResponse>>(SchedulePath(salonId, specialistId, "weekly-availability"), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load weekly availability for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapWeeklyAvailability).ToList();
    }

    public async Task<DomainSchedule.WeeklyAvailability> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<DomainSchedule.TimeInterval> intervals, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new SetWeeklyAvailabilityRequest(intervals.Select(MapIntervalToWire).ToList());

        var response = await apiClient
            .PutAsync<SetWeeklyAvailabilityRequest, WeeklyAvailabilityResponse>(
                SchedulePath(salonId, specialistId, "weekly-availability", DayOfWeekSegment(dayOfWeek)), request, cancellationToken)
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
            .DeleteAsync<object?>(SchedulePath(salonId, specialistId, "weekly-availability", DayOfWeekSegment(dayOfWeek)), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove weekly availability for specialist '{specialistId}' on {dayOfWeek} (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<DomainSchedule.ScheduleOverride>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<ScheduleOverrideResponse>>(SchedulePath(salonId, specialistId, "overrides"), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load schedule overrides for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapOverride).ToList();
    }

    public async Task<DomainSchedule.ScheduleOverride> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<DomainSchedule.TimeInterval> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new SetScheduleOverrideRequest(scheduleDate, intervals.Select(MapIntervalToWire).ToList(), reason);

        var response = await apiClient
            .PutAsync<SetScheduleOverrideRequest, ScheduleOverrideResponse>(
                SchedulePath(salonId, specialistId, "overrides", DateSegment(scheduleDate)), request, cancellationToken)
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
            .DeleteAsync<object?>(SchedulePath(salonId, specialistId, "overrides", overrideId), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove schedule override '{overrideId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<DomainSchedule.SpecialistLeave>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<LeaveResponse>>(SchedulePath(salonId, specialistId, "leaves"), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load leave records for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapLeave).ToList();
    }

    public async Task<DomainSchedule.SpecialistLeave> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new CreateLeaveRequest(startDate, endDate, reason);

        var response = await apiClient
            .PostAsync<CreateLeaveRequest, LeaveResponse>(SchedulePath(salonId, specialistId, "leaves"), request, cancellationToken)
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
            .DeleteAsync<object?>(SchedulePath(salonId, specialistId, "leaves", leaveId), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove leave record '{leaveId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<DomainSchedule.SpecialistBlock>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<BlockResponse>>(SchedulePath(salonId, specialistId, "blocks"), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load blocks for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(MapBlock).ToList();
    }

    public async Task<DomainSchedule.SpecialistBlock> CreateBlockAsync(string specialistId, DateOnly scheduleDate, DomainSchedule.TimeInterval interval, string? reason, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new CreateBlockRequest(scheduleDate, interval.Start, interval.End, reason);

        var response = await apiClient
            .PostAsync<CreateBlockRequest, BlockResponse>(SchedulePath(salonId, specialistId, "blocks"), request, cancellationToken)
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
            .DeleteAsync<object?>(SchedulePath(salonId, specialistId, "blocks", blockId), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove block '{blockId}' for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to load a specialist schedule for.");
    }

    private static string SchedulePath(string salonId, string specialistId, string resource, string? segment = null)
    {
        var basePath = $"/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/{resource}";
        return segment is null ? basePath : $"{basePath}/{segment}";
    }

    /// <summary>Matches Spring's default enum path-variable converter (<c>DayOfWeek.valueOf</c>) exactly - upper-case, case-sensitive.</summary>
    private static string DayOfWeekSegment(DayOfWeek dayOfWeek) => dayOfWeek.ToString().ToUpperInvariant();

    /// <summary>Matches Spring's default <c>LocalDate</c> path-variable converter (ISO-8601).</summary>
    private static string DateSegment(DateOnly scheduleDate) => scheduleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DomainSchedule.TimeInterval MapIntervalToDomain(ScheduleTimeIntervalDto interval) =>
        new(interval.Start, interval.End);

    private static ScheduleTimeIntervalDto MapIntervalToWire(DomainSchedule.TimeInterval interval) =>
        new(interval.Start, interval.End);

    private static DomainSchedule.WeeklyAvailability MapWeeklyAvailability(WeeklyAvailabilityResponse response) => new(
        response.Id,
        response.SpecialistId,
        Enum.Parse<DayOfWeek>(response.DayOfWeek, ignoreCase: true),
        response.Intervals.Select(MapIntervalToDomain).ToList());

    private static DomainSchedule.ScheduleOverride MapOverride(ScheduleOverrideResponse response) => new(
        response.Id,
        response.SpecialistId,
        response.Date,
        response.Intervals.Select(MapIntervalToDomain).ToList(),
        response.Reason);

    private static DomainSchedule.SpecialistLeave MapLeave(LeaveResponse response) => new(
        response.Id,
        response.SpecialistId,
        response.StartDate,
        response.EndDate,
        response.Reason);

    private static DomainSchedule.SpecialistBlock MapBlock(BlockResponse response) => new(
        response.Id,
        response.SpecialistId,
        response.Date,
        new DomainSchedule.TimeInterval(response.Start, response.End),
        response.Reason);
}
