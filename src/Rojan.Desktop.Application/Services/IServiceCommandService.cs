namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Write use cases for the service catalog - deliberately scoped to
/// specialist-to-service assignment only (no create/update-service
/// commands were requested for this phase; catalog authoring is a future
/// concern, not Phase 13's).
/// </summary>
public interface IServiceCommandService
{
    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);
}
