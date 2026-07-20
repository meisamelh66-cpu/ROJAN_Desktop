using Rojan.Desktop.Domain.Help;

namespace Rojan.Desktop.Domain.Tests.Help;

/// <summary>Exercises <see cref="HelpContentRules"/>'s pure context-resolution and version-compatibility logic.</summary>
public sealed class HelpContentRulesTests
{
    private static HelpTopic Topic(string id, string moduleId, string? pageId) =>
        new(id, moduleId, pageId, $"Help_{id}", [], [], "1.0.0");

    [Fact]
    public void ResolveContext_ExactModuleAndPageMatch_ReturnsThatTopic()
    {
        var topics = new[]
        {
            Topic("module-level", "customers", null),
            Topic("page-level", "customers", "detail"),
        };

        var result = HelpContentRules.ResolveContext(topics, "customers", "detail");

        Assert.Equal("page-level", result?.Id);
    }

    [Fact]
    public void ResolveContext_NoPageMatch_FallsBackToModuleLevelTopic()
    {
        var topics = new[]
        {
            Topic("module-level", "customers", null),
            Topic("page-level", "customers", "detail"),
        };

        var result = HelpContentRules.ResolveContext(topics, "customers", "list");

        Assert.Equal("module-level", result?.Id);
    }

    [Fact]
    public void ResolveContext_NoPageIdRequested_ReturnsModuleLevelTopic()
    {
        var topics = new[] { Topic("module-level", "customers", null) };

        var result = HelpContentRules.ResolveContext(topics, "customers", null);

        Assert.Equal("module-level", result?.Id);
    }

    [Fact]
    public void ResolveContext_NoMatchingModule_ReturnsNull()
    {
        var topics = new[] { Topic("module-level", "customers", null) };

        var result = HelpContentRules.ResolveContext(topics, "inventory", null);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    [InlineData("1.5.2", "1.0.0", true)]
    public void IsVersionCompatible_ComparesMajorVersionOnly(string topicVersion, string appVersion, bool expected)
    {
        Assert.Equal(expected, HelpContentRules.IsVersionCompatible(topicVersion, appVersion));
    }

    [Theory]
    [InlineData("not-a-version", "1.0.0")]
    [InlineData("1.0.0", "not-a-version")]
    [InlineData("garbage", "garbage")]
    public void IsVersionCompatible_UnparseableVersion_FailsOpen(string topicVersion, string appVersion)
    {
        Assert.True(HelpContentRules.IsVersionCompatible(topicVersion, appVersion));
    }
}
