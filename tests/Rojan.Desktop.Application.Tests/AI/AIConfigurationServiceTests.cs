using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class AIConfigurationServiceTests
{
    private static AIConfigurationService CreateSut() => new(new StubAIRepository());

    [Fact]
    public async Task GetConfigurationAsync_ReturnsTheSeededMockConfiguration()
    {
        var sut = CreateSut();

        var configuration = await sut.GetConfigurationAsync();

        Assert.Equal(AIProviderType.Mock, configuration.ProviderType);
        Assert.True(configuration.IsEnabled);
    }

    [Fact]
    public async Task SetConfigurationAsync_PersistsAndReturnsTheNewSelection()
    {
        var sut = CreateSut();

        var saved = await sut.SetConfigurationAsync(AIProviderType.Anthropic, "claude-test", false);
        var reloaded = await sut.GetConfigurationAsync();

        Assert.Equal(AIProviderType.Anthropic, saved.ProviderType);
        Assert.Equal("claude-test", saved.ModelId);
        Assert.False(saved.IsEnabled);
        Assert.Equal(saved, reloaded);
    }

    [Fact]
    public void GetAvailableProviderTypes_ListsEveryProviderType()
    {
        var sut = CreateSut();

        var types = sut.GetAvailableProviderTypes();

        Assert.Equal(5, types.Count);
        Assert.Contains(AIProviderType.Mock, types);
        Assert.Contains(AIProviderType.OpenAI, types);
        Assert.Contains(AIProviderType.Anthropic, types);
        Assert.Contains(AIProviderType.AzureOpenAI, types);
        Assert.Contains(AIProviderType.LocalModel, types);
    }
}
