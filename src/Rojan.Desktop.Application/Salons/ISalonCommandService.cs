namespace Rojan.Desktop.Application.Salons;

/// <summary>Input to <see cref="ISalonCommandService.CreateSalonAsync"/> - field-for-field matches ROJAN_Backend's own <c>CreateSalonRequest</c> validation shape (name/phone/address required, description/email optional).</summary>
public sealed record CreateSalonCommand(string Name, string? Description, string Phone, string? Email, string Address);

/// <summary>
/// Write use case for the Salon vertical slice - deliberately scoped to
/// just creation (Phase 1.2 Owner App Create Salon Flow). Update/deactivate
/// already exist on ROJAN_Backend (<c>PUT</c>/<c>DELETE /api/v1/salons/{id}</c>)
/// but have no Owner App consumer yet - out of this phase's scope, not
/// forgotten.
/// </summary>
public interface ISalonCommandService
{
    public Task<SalonDto> CreateSalonAsync(CreateSalonCommand command, CancellationToken cancellationToken = default);
}
