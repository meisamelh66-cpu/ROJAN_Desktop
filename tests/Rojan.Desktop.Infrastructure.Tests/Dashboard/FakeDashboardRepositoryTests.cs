using Rojan.Desktop.Infrastructure.Dashboard;

namespace Rojan.Desktop.Infrastructure.Tests.Dashboard;

/// <summary>Smoke coverage only - see the equivalent note on Customers.FakeCustomerRepositoryTests.</summary>
public sealed class FakeDashboardRepositoryTests
{
    [Fact]
    public async Task GetKpiMetricsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeDashboardRepository();

        var result = await sut.GetKpiMetricsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeDashboardRepository();

        var result = await sut.GetRecentActivityAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetKpiMetricsAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeDashboardRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetKpiMetricsAsync(cts.Token));
    }
}
