using Rojan.Desktop.Domain.Services;
using Rojan.Desktop.Infrastructure.Services;

namespace Rojan.Desktop.Infrastructure.Tests.Services;

/// <summary>Smoke + behavioral coverage - same reasoning as Customers.FakeCustomerRepositoryTests.</summary>
public sealed class FakeServiceRepositoryTests
{
    [Fact]
    public async Task GetServicesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeServiceRepository();

        var result = await sut.GetServicesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetServicesAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeServiceRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetServicesAsync(cts.Token));
    }

    [Fact]
    public async Task GetServiceByIdAsync_KnownId_ReturnsMatchingService()
    {
        var sut = new FakeServiceRepository();

        var service = await sut.GetServiceByIdAsync("service-1");

        Assert.NotNull(service);
        Assert.Equal("service-1", service.Id);
    }

    [Fact]
    public async Task GetServiceByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeServiceRepository();

        var service = await sut.GetServiceByIdAsync("no-such-service");

        Assert.Null(service);
    }

    [Fact]
    public async Task GetAssignedSpecialistsAsync_KnownService_ReturnsOnlyThatServicesAssignments()
    {
        var sut = new FakeServiceRepository();

        var assignments = await sut.GetAssignedSpecialistsAsync("service-1");

        Assert.NotEmpty(assignments);
        Assert.All(assignments, assignment => Assert.Equal("service-1", assignment.ServiceId));
    }

    [Fact]
    public async Task AssignSpecialistAsync_NewAssignment_BecomesVisibleViaGetAssignedSpecialistsAsync()
    {
        var sut = new FakeServiceRepository();
        var assignment = new SpecialistService("assignment-new", "service-1", "specialist-new", "New Specialist");

        await sut.AssignSpecialistAsync(assignment);
        var assignments = await sut.GetAssignedSpecialistsAsync("service-1");

        Assert.Contains(assignments, a => a.Id == "assignment-new");
    }

    [Fact]
    public async Task UnassignSpecialistAsync_ExistingAssignment_NoLongerReturnedByGetAssignedSpecialistsAsync()
    {
        var sut = new FakeServiceRepository();
        var assignment = new SpecialistService("assignment-new", "service-1", "specialist-new", "New Specialist");
        await sut.AssignSpecialistAsync(assignment);

        await sut.UnassignSpecialistAsync("service-1", "assignment-new");
        var assignments = await sut.GetAssignedSpecialistsAsync("service-1");

        Assert.DoesNotContain(assignments, a => a.Id == "assignment-new");
    }
}
