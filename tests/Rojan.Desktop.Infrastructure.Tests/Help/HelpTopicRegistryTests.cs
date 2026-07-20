using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Infrastructure.Help;

namespace Rojan.Desktop.Infrastructure.Tests.Help;

/// <summary>Exercises <see cref="HelpTopicRegistry"/>'s seeded topics - every flagship module is present, ids are unique, and the well-known default topic exists (the fallback <see cref="HelpQueryService.DefaultTopicId"/> depends on).</summary>
public sealed class HelpTopicRegistryTests
{
    private readonly HelpTopicRegistry _registry = new();

    [Fact]
    public async Task GetAllAsync_SeedsTheDefaultTopic()
    {
        var topics = await _registry.GetAllAsync();

        Assert.Contains(topics, topic => topic.Id == HelpQueryService.DefaultTopicId);
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("customers")]
    [InlineData("bookings")]
    [InlineData("inventory")]
    [InlineData("accounting")]
    [InlineData("services")]
    public async Task GetAllAsync_SeedsEveryFlagshipModule(string moduleId)
    {
        var topics = await _registry.GetAllAsync();

        Assert.Contains(topics, topic => topic.ModuleId == moduleId);
    }

    [Fact]
    public async Task GetAllAsync_EveryTopicIdIsUnique()
    {
        var topics = await _registry.GetAllAsync();

        var distinctIds = topics.Select(topic => topic.Id).Distinct().Count();
        Assert.Equal(topics.Count, distinctIds);
    }

    [Fact]
    public async Task GetAllAsync_EveryTopicHasANonEmptyKeyPrefixAndAtLeastOneShortcut()
    {
        var topics = await _registry.GetAllAsync();

        Assert.All(topics, topic =>
        {
            Assert.False(string.IsNullOrWhiteSpace(topic.KeyPrefix));
            Assert.NotEmpty(topic.Shortcuts);
        });
    }

    [Fact]
    public async Task GetByIdAsync_KnownId_ReturnsTheMatchingTopic()
    {
        var topic = await _registry.GetByIdAsync("help-customers");

        Assert.NotNull(topic);
        Assert.Equal("customers", topic!.ModuleId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var topic = await _registry.GetByIdAsync("does-not-exist");

        Assert.Null(topic);
    }

    [Fact]
    public async Task GetAllAsync_EveryRelatedTopicIdReferencesATopicThatActuallyExists()
    {
        var topics = await _registry.GetAllAsync();
        var ids = topics.Select(topic => topic.Id).ToHashSet();

        Assert.All(topics, topic => Assert.All(topic.RelatedTopicIds, relatedId => Assert.Contains(relatedId, ids)));
    }
}
