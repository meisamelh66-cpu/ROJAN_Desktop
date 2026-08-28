using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubSpecialistCommandService : ISpecialistCommandService
{
    public List<CreateSpecialistRequest> CreateRequests { get; } = [];

    public List<UpdateSpecialistRequest> UpdateRequests { get; } = [];

    public List<(string SpecialistId, string Name)> AddSkillCalls { get; } = [];

    public List<(string SpecialistId, string SkillId)> RemoveSkillCalls { get; } = [];

    public List<(string SpecialistId, string ServiceId)> AssignServiceCalls { get; } = [];

    public List<(string SpecialistId, string ServiceId)> RemoveServiceAssignmentCalls { get; } = [];

    /// <summary>Optional hook run after a specialist is created, before the DTO is returned - lets a test mirror the created specialist into whatever backing list the paired query-service stub reads from.</summary>
    public Action<CreateSpecialistRequest, SpecialistDto>? OnSpecialistCreated { get; set; }

    /// <summary>Specialist Deactivation Wiring: when set, <see cref="UpdateSpecialistAsync"/> throws this instead of succeeding - lets a test drive SpecialistProfileViewModel's save-failure path (e.g. the still-unsupported Inactive -&gt; Active/OnLeave directions, or a plain backend failure).</summary>
    public Exception? UpdateSpecialistException { get; set; }

    /// <summary>Specialist-Service Assignment: when set, <see cref="AssignServiceAsync"/> throws this instead of succeeding - same reasoning as <see cref="UpdateSpecialistException"/>.</summary>
    public Exception? AssignServiceException { get; set; }

    /// <summary>Specialist-Service Assignment: when set, <see cref="RemoveServiceAssignmentAsync"/> throws this instead of succeeding.</summary>
    public Exception? RemoveServiceAssignmentException { get; set; }

    /// <summary>Production Hardening (missing-guard sweep): when set, <see cref="CreateSpecialistAsync"/> throws this instead of succeeding.</summary>
    public Exception? CreateSpecialistException { get; set; }

    /// <summary>Production Hardening (missing-guard sweep): when set, <see cref="AddSkillAsync"/> throws this instead of succeeding.</summary>
    public Exception? AddSkillException { get; set; }

    /// <summary>Production Hardening (missing-guard sweep): when set, <see cref="RemoveSkillAsync"/> throws this instead of succeeding.</summary>
    public Exception? RemoveSkillException { get; set; }

    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        if (CreateSpecialistException is not null)
        {
            return Task.FromException<SpecialistDto>(CreateSpecialistException);
        }

        var dto = new SpecialistDto(
            "new-specialist", request.FullName, request.Title, request.Email, request.Phone,
            SpecialistStatus.Active, request.Bio);
        OnSpecialistCreated?.Invoke(request, dto);
        return Task.FromResult(dto);
    }

    public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        UpdateRequests.Add(request);

        if (UpdateSpecialistException is not null)
        {
            return Task.FromException<SpecialistDto>(UpdateSpecialistException);
        }

        return Task.FromResult(new SpecialistDto(
            request.Id, request.FullName, request.Title, request.Email, request.Phone,
            request.Status, request.Bio));
    }

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default)
    {
        AddSkillCalls.Add((specialistId, name));
        return AddSkillException is not null
            ? Task.FromException<SpecialistSkillDto>(AddSkillException)
            : Task.FromResult(new SpecialistSkillDto("new-skill", specialistId, name));
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        RemoveSkillCalls.Add((specialistId, skillId));
        return RemoveSkillException is not null ? Task.FromException(RemoveSkillException) : Task.CompletedTask;
    }

    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        AssignServiceCalls.Add((specialistId, serviceId));
        return AssignServiceException is null ? Task.CompletedTask : Task.FromException(AssignServiceException);
    }

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        RemoveServiceAssignmentCalls.Add((specialistId, serviceId));
        return RemoveServiceAssignmentException is null ? Task.CompletedTask : Task.FromException(RemoveServiceAssignmentException);
    }
}
