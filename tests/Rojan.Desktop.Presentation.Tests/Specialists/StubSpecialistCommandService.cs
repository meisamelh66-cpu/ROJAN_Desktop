using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubSpecialistCommandService : ISpecialistCommandService
{
    public List<CreateSpecialistRequest> CreateRequests { get; } = [];

    public List<UpdateSpecialistRequest> UpdateRequests { get; } = [];

    public List<(string SpecialistId, string Name)> AddSkillCalls { get; } = [];

    public List<(string SpecialistId, string SkillId)> RemoveSkillCalls { get; } = [];

    /// <summary>Optional hook run after a specialist is created, before the DTO is returned - lets a test mirror the created specialist into whatever backing list the paired query-service stub reads from.</summary>
    public Action<CreateSpecialistRequest, SpecialistDto>? OnSpecialistCreated { get; set; }

    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        var dto = new SpecialistDto(
            "new-specialist", request.FullName, request.Title, request.Email, request.Phone,
            SpecialistStatus.Active, request.Bio);
        OnSpecialistCreated?.Invoke(request, dto);
        return Task.FromResult(dto);
    }

    public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        UpdateRequests.Add(request);
        return Task.FromResult(new SpecialistDto(
            request.Id, request.FullName, request.Title, request.Email, request.Phone,
            request.Status, request.Bio));
    }

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default)
    {
        AddSkillCalls.Add((specialistId, name));
        return Task.FromResult(new SpecialistSkillDto("new-skill", specialistId, name));
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        RemoveSkillCalls.Add((specialistId, skillId));
        return Task.CompletedTask;
    }
}
