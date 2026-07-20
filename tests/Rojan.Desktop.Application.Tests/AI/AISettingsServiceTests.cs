using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class AISettingsServiceTests
{
    private static AISettingsService CreateSut() => new(new StubAIRepository());

    [Fact]
    public async Task GetSettingsAsync_ReturnsTheSeededDefaults()
    {
        var sut = CreateSut();

        var settings = await sut.GetSettingsAsync();

        Assert.True(settings.InsightsEnabled);
        Assert.True(settings.SmartNotificationsEnabled);
        Assert.True(settings.DailySummaryEnabled);
        Assert.True(settings.AutoGenerateRecommendations);
    }

    [Fact]
    public async Task UpdateSettingsAsync_PersistsTheNewToggleValues()
    {
        var sut = CreateSut();
        var updated = new AISettingsDto(false, false, true, false);

        var saved = await sut.UpdateSettingsAsync(updated);
        var reloaded = await sut.GetSettingsAsync();

        Assert.Equal(updated, saved);
        Assert.Equal(updated, reloaded);
    }
}
