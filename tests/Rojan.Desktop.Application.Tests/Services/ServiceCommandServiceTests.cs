using Rojan.Desktop.Application.Services;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

public sealed class ServiceCommandServiceTests
{
    private static DomainServices.Service MakeService(string id = "service-1") =>
        new(id, "Haircut & Style", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active,
            60, "$65", "Classic cut and blow-dry finish.");

    [Fact]
    public async Task AssignSpecialistAsync_ValidName_AddsAssignment()
    {
        var repository = new StubServiceRepository([MakeService()]);
        var sut = new ServiceCommandService(repository);

        var assignment = await sut.AssignSpecialistAsync("service-1", "Jordan Lee");

        Assert.Equal("service-1", assignment.ServiceId);
        Assert.Equal("Jordan Lee", assignment.SpecialistName);
        Assert.Single(repository.Assignments);
    }

    [Fact]
    public async Task UnassignSpecialistAsync_ExistingAssignment_RemovesAssignment()
    {
        var repository = new StubServiceRepository([MakeService()]);
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));
        var sut = new ServiceCommandService(repository);

        await sut.UnassignSpecialistAsync("service-1", "assignment-1");

        Assert.Empty(repository.Assignments);
    }

    [Fact]
    public async Task CreateServiceAsync_ValidRequest_PassesCategoryIdAndInvariantPriceToRepository()
    {
        var repository = new StubServiceRepository();
        var sut = new ServiceCommandService(repository);
        var request = new CreateServiceRequest("Manicure", "category-1", 45, 400000m, "Classic manicure.");

        var created = await sut.CreateServiceAsync(request);

        Assert.Equal("service-new", created.Id);
        Assert.Equal("Manicure", created.Name);
        var call = Assert.Single(repository.CreateCalls);
        Assert.Equal("category-1", call.CategoryId);
        Assert.Equal("400000", call.Price);
        Assert.Equal(DomainServices.ServiceStatus.Active, call.Status);
    }

    [Fact]
    public async Task UpdateServiceAsync_ExistingService_PreservesCategoryIdFromExisting()
    {
        var repository = new StubServiceRepository([MakeService() with { CategoryId = "category-9" }]);
        var sut = new ServiceCommandService(repository);
        var request = new UpdateServiceRequest("service-1", "Haircut & Style (updated)", 75, 700000m, "Updated description.");

        var updated = await sut.UpdateServiceAsync(request);

        Assert.Equal("Haircut & Style (updated)", updated.Name);
        var call = Assert.Single(repository.UpdateCalls);
        Assert.Equal("category-9", call.CategoryId);
        Assert.Equal(75, call.DurationMinutes);
        Assert.Equal("700000", call.Price);
    }

    [Fact]
    public async Task UpdateServiceAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubServiceRepository();
        var sut = new ServiceCommandService(repository);
        var request = new UpdateServiceRequest("no-such-service", "Name", 30, 100m, "Description");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateServiceAsync(request));

        Assert.Empty(repository.UpdateCalls);
    }

    [Fact]
    public async Task DeactivateServiceAsync_ExistingService_PassesResolvedCategoryIdToRepository()
    {
        var repository = new StubServiceRepository([MakeService() with { CategoryId = "category-9" }]);
        var sut = new ServiceCommandService(repository);

        await sut.DeactivateServiceAsync("service-1");

        var call = Assert.Single(repository.DeactivateCalls);
        Assert.Equal("category-9", call.CategoryId);
        Assert.Equal("service-1", call.ServiceId);
    }

    [Fact]
    public async Task DeactivateServiceAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubServiceRepository();
        var sut = new ServiceCommandService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeactivateServiceAsync("no-such-service"));

        Assert.Empty(repository.DeactivateCalls);
    }
}
