using Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

/// <summary>In-memory, mutable <see cref="ISalonRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubSalonRepository : ISalonRepository
{
    public List<Salon> Salons { get; } = [];

    public Func<Salon, Salon>? OnCreate { get; set; }

    /// <summary>QR Ecosystem (Desktop Productionization Sprint 1): the bytes <see cref="GetQrCodeAsync"/> returns - defaults to a small non-empty placeholder so tests that don't care about the exact content still get something byte-comparable.</summary>
    public byte[] QrCodeBytes { get; set; } = [1, 2, 3];

    public string? LastQrCodeSalonId { get; private set; }

    public int? LastQrCodeSizePx { get; private set; }

    public StubSalonRepository()
    {
    }

    public StubSalonRepository(IReadOnlyList<Salon> salons)
    {
        Salons.AddRange(salons);
    }

    public Task<IReadOnlyList<Salon>> GetMineAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Salon>>(Salons.ToList());

    public Task<Salon> CreateAsync(Salon salon, CancellationToken cancellationToken = default)
    {
        var created = OnCreate?.Invoke(salon) ?? salon with { Id = "salon-server-id" };
        Salons.Add(created);
        return Task.FromResult(created);
    }

    public Task<byte[]> GetQrCodeAsync(string salonId, int sizePx, CancellationToken cancellationToken = default)
    {
        LastQrCodeSalonId = salonId;
        LastQrCodeSizePx = sizePx;
        return Task.FromResult(QrCodeBytes);
    }
}
