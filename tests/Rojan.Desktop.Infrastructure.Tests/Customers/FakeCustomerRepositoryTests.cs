using Rojan.Desktop.Infrastructure.Customers;

namespace Rojan.Desktop.Infrastructure.Tests.Customers;

/// <summary>
/// Smoke coverage only - FakeCustomerRepository is a hardcoded stand-in by
/// design (see its own doc comment), so there is no mapping/business logic
/// here to exercise beyond "it returns data" and "it honors cancellation."
/// This project earns real weight once a live backend repository replaces
/// the fake.
/// </summary>
public sealed class FakeCustomerRepositoryTests
{
    [Fact]
    public async Task GetCustomersAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeCustomerRepository();

        var result = await sut.GetCustomersAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetCustomersAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeCustomerRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetCustomersAsync(cts.Token));
    }
}
