using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class ConversationManagerTests
{
    private static ConversationManager CreateSut() => new(new StubAIRepository());

    [Fact]
    public async Task CreateSessionAsync_DerivesTitleFromFirstMessage()
    {
        var sut = CreateSut();

        var session = await sut.CreateSessionAsync("How is revenue trending this month?");

        Assert.Equal("How is revenue trending this month?", session.Title);
        Assert.False(session.IsPinned);
    }

    [Fact]
    public async Task AppendMessageAsync_AddsMessageAndTouchesSessionUpdatedAt()
    {
        var sut = CreateSut();
        var session = await sut.CreateSessionAsync("New conversation");

        var message = await sut.AppendMessageAsync(session.Id, ConversationRole.User, "Hello", 3);
        var messages = await sut.GetMessagesAsync(session.Id);
        var sessions = await sut.GetSessionsAsync();

        Assert.Single(messages);
        Assert.Equal("Hello", message.Content);
        Assert.Equal(ConversationRole.User, message.Role);
        Assert.True(sessions.Single(s => s.Id == session.Id).UpdatedAt >= session.CreatedAt);
    }

    [Fact]
    public async Task TogglePinAsync_PinsThenUnpinsSession()
    {
        var sut = CreateSut();
        var session = await sut.CreateSessionAsync("New conversation");

        var pinned = await sut.TogglePinAsync(session.Id);
        var unpinned = await sut.TogglePinAsync(session.Id);

        Assert.True(pinned.IsPinned);
        Assert.False(unpinned.IsPinned);
    }

    [Fact]
    public async Task TogglePinAsync_WhenAtMaxPinnedSessions_Throws()
    {
        var sut = CreateSut();
        for (var i = 0; i < 10; i++)
        {
            var session = await sut.CreateSessionAsync($"Conversation {i}");
            await sut.TogglePinAsync(session.Id);
        }

        var overflow = await sut.CreateSessionAsync("One too many");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.TogglePinAsync(overflow.Id));
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesSessionAndItsMessages()
    {
        var sut = CreateSut();
        var session = await sut.CreateSessionAsync("New conversation");
        await sut.AppendMessageAsync(session.Id, ConversationRole.User, "Hi", 1);

        await sut.DeleteSessionAsync(session.Id);

        Assert.Empty(await sut.GetSessionsAsync());
        Assert.Empty(await sut.GetMessagesAsync(session.Id));
    }

    [Fact]
    public async Task SearchSessionsAsync_MatchesTitleOrMessageContent()
    {
        var sut = CreateSut();
        var revenueSession = await sut.CreateSessionAsync("Revenue question");
        var otherSession = await sut.CreateSessionAsync("Unrelated topic");
        await sut.AppendMessageAsync(otherSession.Id, ConversationRole.User, "What about inventory levels?", 5);

        var revenueResults = await sut.SearchSessionsAsync("revenue");
        var inventoryResults = await sut.SearchSessionsAsync("inventory");

        Assert.Contains(revenueResults, s => s.Id == revenueSession.Id);
        Assert.Contains(inventoryResults, s => s.Id == otherSession.Id);
    }

    [Fact]
    public async Task ExportSessionAsync_IncludesTitleAndEveryMessage()
    {
        var sut = CreateSut();
        var session = await sut.CreateSessionAsync("New conversation");
        await sut.AppendMessageAsync(session.Id, ConversationRole.User, "Hi there", 2);
        await sut.AppendMessageAsync(session.Id, ConversationRole.Assistant, "Hello!", 2);

        var export = await sut.ExportSessionAsync(session.Id);

        Assert.Contains("New conversation", export, StringComparison.Ordinal);
        Assert.Contains("Hi there", export, StringComparison.Ordinal);
        Assert.Contains("Hello!", export, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClearHistoryAsync_DeletesOnlyUnpinnedSessions()
    {
        var sut = CreateSut();
        var pinned = await sut.CreateSessionAsync("Keep me");
        await sut.TogglePinAsync(pinned.Id);
        var unpinned = await sut.CreateSessionAsync("Delete me");

        await sut.ClearHistoryAsync();
        var remaining = await sut.GetSessionsAsync();

        Assert.Contains(remaining, s => s.Id == pinned.Id);
        Assert.DoesNotContain(remaining, s => s.Id == unpinned.Id);
    }
}
