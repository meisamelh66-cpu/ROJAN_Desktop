using Rojan.Desktop.Domain.AI;
using Rojan.Desktop.Infrastructure.AI;

namespace Rojan.Desktop.Infrastructure.Tests.AI;

public sealed class FakeAIRepositoryTests
{
    [Fact]
    public async Task GetSessionsAsync_ReturnsTheTwoSeededSessionsNewestFirst()
    {
        var repository = new FakeAIRepository();

        var sessions = await repository.GetSessionsAsync();

        Assert.Equal(2, sessions.Count);
        Assert.Equal("session-1", sessions[0].Id);
        Assert.True(sessions[0].IsPinned);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsMessagesForThatSessionOnlyInChronologicalOrder()
    {
        var repository = new FakeAIRepository();

        var messages = await repository.GetMessagesAsync("session-1");

        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("session-1", m.SessionId));
        Assert.Equal(ConversationRole.User, messages[0].Role);
        Assert.Equal(ConversationRole.Assistant, messages[1].Role);
    }

    [Fact]
    public async Task GetPromptTemplatesAsync_ReturnsOneSystemDefinedTemplatePerCategory()
    {
        var repository = new FakeAIRepository();

        var templates = await repository.GetPromptTemplatesAsync();

        Assert.Equal(Enum.GetValues<InsightCategory>().Length, templates.Count);
        Assert.All(templates, t => Assert.True(t.IsSystemDefined));
        Assert.Contains(templates, t => t.Category == InsightCategory.General);
    }

    [Fact]
    public async Task GetProviderConfigurationAsync_DefaultsToTheMockProviderEnabled()
    {
        var repository = new FakeAIRepository();

        var configuration = await repository.GetProviderConfigurationAsync();

        Assert.Equal(AIProviderType.Mock, configuration.ProviderType);
        Assert.True(configuration.IsEnabled);
    }

    [Fact]
    public async Task SetProviderConfigurationAsync_PersistsTheChange()
    {
        var repository = new FakeAIRepository();

        await repository.SetProviderConfigurationAsync(new AIProviderConfiguration(AIProviderType.OpenAI, "gpt-test", false));
        var reloaded = await repository.GetProviderConfigurationAsync();

        Assert.Equal(AIProviderType.OpenAI, reloaded.ProviderType);
        Assert.False(reloaded.IsEnabled);
    }

    [Fact]
    public async Task CreateSessionAsync_ThenDeleteSessionAsync_AlsoRemovesItsMessages()
    {
        var repository = new FakeAIRepository();
        var now = DateTimeOffset.Now;
        var session = await repository.CreateSessionAsync(new ConversationSession("session-new", "New", now, now, false));
        await repository.AddMessageAsync(new ConversationMessage("message-new", session.Id, ConversationRole.User, "Hi", now, 1));

        await repository.DeleteSessionAsync(session.Id);

        Assert.Empty(await repository.GetMessagesAsync(session.Id));
        Assert.DoesNotContain(await repository.GetSessionsAsync(), s => s.Id == session.Id);
    }

    [Fact]
    public async Task GetTokenUsageAsync_ReturnsTheSeededUsageNewestFirst()
    {
        var repository = new FakeAIRepository();

        var usage = await repository.GetTokenUsageAsync();

        Assert.Equal(2, usage.Count);
        Assert.True(usage[0].RecordedAt >= usage[1].RecordedAt);
    }
}
