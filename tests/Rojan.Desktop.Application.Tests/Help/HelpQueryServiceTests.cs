using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Domain.Help;

namespace Rojan.Desktop.Application.Tests.Help;

/// <summary>Exercises <see cref="HelpQueryService"/>'s context resolution, default-topic fallback, and version-compatibility filtering.</summary>
public sealed class HelpQueryServiceTests
{
    private static HelpTopic Topic(string id, string moduleId, string? pageId, string version = "1.0.0") =>
        new(id, moduleId, pageId, $"Help_{id}", [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")], [], version);

    [Fact]
    public async Task GetAllTopicsAsync_MapsEveryDomainTopicToADto()
    {
        var service = new HelpQueryService(new StubHelpRepository([Topic("help-dashboard", "dashboard", null)]));

        var topics = await service.GetAllTopicsAsync();

        var topic = Assert.Single(topics);
        Assert.Equal("help-dashboard", topic.Id);
        Assert.Equal("dashboard", topic.ModuleId);
        Assert.Equal("Help_help-dashboard", topic.KeyPrefix);
        var shortcut = Assert.Single(topic.Shortcuts);
        Assert.Equal("Esc", shortcut.KeysDisplay);
    }

    [Fact]
    public async Task GetAllTopicsAsync_ExcludesVersionIncompatibleTopics()
    {
        var service = new HelpQueryService(new StubHelpRepository([
            Topic("compatible", "dashboard", null, "1.0.0"),
            Topic("too-new", "customers", null, "2.0.0"),
        ]));

        var topics = await service.GetAllTopicsAsync();

        Assert.Single(topics);
        Assert.Equal("compatible", topics[0].Id);
    }

    [Fact]
    public async Task GetTopicByIdAsync_UnknownId_ReturnsNull()
    {
        var service = new HelpQueryService(new StubHelpRepository([Topic("help-dashboard", "dashboard", null)]));

        var topic = await service.GetTopicByIdAsync("does-not-exist");

        Assert.Null(topic);
    }

    [Fact]
    public async Task GetTopicByIdAsync_VersionIncompatible_ReturnsNull()
    {
        var service = new HelpQueryService(new StubHelpRepository([Topic("too-new", "dashboard", null, "2.0.0")]));

        var topic = await service.GetTopicByIdAsync("too-new");

        Assert.Null(topic);
    }

    [Fact]
    public async Task GetTopicForContextAsync_ExactModuleMatch_ReturnsThatTopic()
    {
        var service = new HelpQueryService(new StubHelpRepository([
            Topic("help-customers", "customers", null),
            Topic(HelpQueryService.DefaultTopicId, "default", null),
        ]));

        var topic = await service.GetTopicForContextAsync("customers");

        Assert.Equal("help-customers", topic?.Id);
    }

    [Fact]
    public async Task GetTopicForContextAsync_NoModuleMatch_FallsBackToDefaultTopic()
    {
        var service = new HelpQueryService(new StubHelpRepository([
            Topic("help-customers", "customers", null),
            Topic(HelpQueryService.DefaultTopicId, "default", null),
        ]));

        var topic = await service.GetTopicForContextAsync("settings");

        Assert.Equal(HelpQueryService.DefaultTopicId, topic?.Id);
    }

    [Fact]
    public async Task GetTopicForContextAsync_NoModuleMatchAndNoDefaultTopic_ReturnsNull()
    {
        var service = new HelpQueryService(new StubHelpRepository([Topic("help-customers", "customers", null)]));

        var topic = await service.GetTopicForContextAsync("settings");

        Assert.Null(topic);
    }
}
