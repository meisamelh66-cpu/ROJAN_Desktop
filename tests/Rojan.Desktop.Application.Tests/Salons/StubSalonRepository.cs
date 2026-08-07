using Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

/// <summary>In-memory, mutable <see cref="ISalonRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubSalonRepository : ISalonRepository
{
    public List<Salon> Salons { get; } = [];

    public Func<Salon, Salon>? OnCreate { get; set; }

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
}
