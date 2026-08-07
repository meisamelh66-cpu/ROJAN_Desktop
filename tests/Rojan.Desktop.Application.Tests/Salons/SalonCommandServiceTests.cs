using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

public sealed class SalonCommandServiceTests
{
    [Fact]
    public async Task CreateSalonAsync_SendsEveryFieldToTheRepository()
    {
        var repository = new StubSalonRepository();
        var sut = new SalonCommandService(repository, new StubSalonContextService());
        var command = new CreateSalonCommand("Glow Salon", "Full-service salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St");

        var result = await sut.CreateSalonAsync(command);

        var sent = Assert.Single(repository.Salons);
        Assert.Equal("Glow Salon", sent.Name);
        Assert.Equal("Full-service salon.", sent.Description);
        Assert.Equal("+1 555 0100", sent.Phone);
        Assert.Equal("hello@glowsalon.example", sent.Email);
        Assert.Equal("1 Main St", sent.Address);
        Assert.Equal("salon-server-id", result.Id);
    }

    [Fact]
    public async Task CreateSalonAsync_BlankOptionalFields_SentAsNull()
    {
        var repository = new StubSalonRepository();
        var sut = new SalonCommandService(repository, new StubSalonContextService());
        var command = new CreateSalonCommand("Glow Salon", "   ", "+1 555 0100", string.Empty, "1 Main St");

        await sut.CreateSalonAsync(command);

        var sent = Assert.Single(repository.Salons);
        Assert.Null(sent.Description);
        Assert.Null(sent.Email);
    }

    [Fact]
    public async Task CreateSalonAsync_ReturnsTheServerAssignedSalonMappedToDto()
    {
        var repository = new StubSalonRepository
        {
            OnCreate = salon => salon with { Id = "real-backend-id", Active = true },
        };
        var sut = new SalonCommandService(repository, new StubSalonContextService());
        var command = new CreateSalonCommand("Glow Salon", null, "+1 555 0100", null, "1 Main St");

        var result = await sut.CreateSalonAsync(command);

        Assert.Equal("real-backend-id", result.Id);
        Assert.True(result.Active);
    }

    [Fact]
    public async Task CreateSalonAsync_InvalidatesTheSalonContextCacheOnSuccess()
    {
        var repository = new StubSalonRepository();
        var salonContextService = new StubSalonContextService();
        var sut = new SalonCommandService(repository, salonContextService);
        var command = new CreateSalonCommand("Glow Salon", null, "+1 555 0100", null, "1 Main St");

        await sut.CreateSalonAsync(command);

        Assert.Equal(1, salonContextService.InvalidateCallCount);
    }
}
