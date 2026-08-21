using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Presentation.Tests.Salons;

/// <summary>Configurable <see cref="ISalonQueryService"/> test double - same reasoning as Services.StubServiceQueryService.</summary>
internal sealed class StubSalonQueryService(Func<CancellationToken, Task<SalonDto?>> getMySalon) : ISalonQueryService
{
    /// <summary>QR Ecosystem (Desktop Productionization Sprint 1): settable so a QR-page test can exercise <see cref="GetSalonQrCodeAsync"/> without needing a full delegate constructor overload - defaults to <see cref="NotSupportedException"/>, same "not used by tests that don't ask for it" convention every other stub in this suite already follows.</summary>
    public Func<int, CancellationToken, Task<byte[]>>? GetSalonQrCode { get; set; }

    public Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default) => getMySalon(cancellationToken);

    public Task<byte[]> GetSalonQrCodeAsync(int sizePx, CancellationToken cancellationToken = default) =>
        GetSalonQrCode?.Invoke(sizePx, cancellationToken) ?? throw new NotSupportedException("This test did not configure GetSalonQrCode.");
}
