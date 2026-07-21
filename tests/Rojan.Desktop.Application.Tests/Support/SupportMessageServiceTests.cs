using Rojan.Desktop.Application.Support;

namespace Rojan.Desktop.Application.Tests.Support;

public sealed class SupportMessageServiceTests
{
    [Fact]
    public async Task SubmitAsync_ValidMessage_PersistsAndReturnsIt()
    {
        var repository = new FakeSupportMessageRepository();
        var service = new SupportMessageService(repository);

        var message = await service.SubmitAsync(SupportMessageType.General, "Subject", "Body", "Sara", "sara@example.com");

        Assert.Equal(SupportMessageType.General, message.Type);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SubmitAsync_MissingSubject_Throws()
    {
        var service = new SupportMessageService(new FakeSupportMessageRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(SupportMessageType.BugReport, "", "Body", "Sara", "sara@example.com"));
    }

    [Fact]
    public async Task SubmitAsync_SuperAdminType_PersistsWithThatType()
    {
        var repository = new FakeSupportMessageRepository();
        var service = new SupportMessageService(repository);

        var message = await service.SubmitAsync(SupportMessageType.SuperAdmin, "Urgent", "Please review", "Sara", "sara@example.com");

        Assert.Equal(SupportMessageType.SuperAdmin, message.Type);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMostRecentFirst()
    {
        var repository = new FakeSupportMessageRepository();
        var service = new SupportMessageService(repository);
        await service.SubmitAsync(SupportMessageType.General, "First", "Body", "Sara", "sara@example.com");
        await Task.Delay(10);
        var second = await service.SubmitAsync(SupportMessageType.Suggestion, "Second", "Body", "Sara", "sara@example.com");

        var all = await service.GetAllAsync();

        Assert.Equal(second.Id, all[0].Id);
    }
}
