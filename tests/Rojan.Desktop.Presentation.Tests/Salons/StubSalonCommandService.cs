using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Presentation.Tests.Salons;

/// <summary>Configurable, call-recording <see cref="ISalonCommandService"/> test double - same reasoning as Calendar.StubCalendarQueryService/StubCalendarCommandService.</summary>
internal sealed class StubSalonCommandService(Func<CreateSalonCommand, CancellationToken, Task<SalonDto>>? createSalon = null) : ISalonCommandService
{
    private readonly Func<CreateSalonCommand, CancellationToken, Task<SalonDto>> _createSalon =
        createSalon ?? ((command, _) => Task.FromResult(new SalonDto("salon-new", command.Name, command.Description ?? string.Empty, command.Phone, command.Email ?? string.Empty, command.Address, true)));

    public List<CreateSalonCommand> CreateCalls { get; } = [];

    public Task<SalonDto> CreateSalonAsync(CreateSalonCommand command, CancellationToken cancellationToken = default)
    {
        CreateCalls.Add(command);
        return _createSalon(command, cancellationToken);
    }
}
