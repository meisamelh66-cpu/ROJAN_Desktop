using Rojan.Desktop.Application.AI;
using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class AIMapperTests
{
    [Theory]
    [InlineData(DomainAI.ConversationRole.System, ConversationRole.System)]
    [InlineData(DomainAI.ConversationRole.Developer, ConversationRole.Developer)]
    [InlineData(DomainAI.ConversationRole.User, ConversationRole.User)]
    [InlineData(DomainAI.ConversationRole.Assistant, ConversationRole.Assistant)]
    public void MapRole_RoundTripsEveryValue(DomainAI.ConversationRole domainRole, ConversationRole expected)
    {
        var mapped = AIMapper.MapRole(domainRole);
        Assert.Equal(expected, mapped);
        Assert.Equal(domainRole, AIMapper.MapRole(mapped));
    }

    [Theory]
    [InlineData(DomainAI.AIProviderType.Mock, AIProviderType.Mock)]
    [InlineData(DomainAI.AIProviderType.OpenAI, AIProviderType.OpenAI)]
    [InlineData(DomainAI.AIProviderType.Anthropic, AIProviderType.Anthropic)]
    [InlineData(DomainAI.AIProviderType.AzureOpenAI, AIProviderType.AzureOpenAI)]
    [InlineData(DomainAI.AIProviderType.LocalModel, AIProviderType.LocalModel)]
    public void MapProviderType_RoundTripsEveryValue(DomainAI.AIProviderType domainType, AIProviderType expected)
    {
        var mapped = AIMapper.MapProviderType(domainType);
        Assert.Equal(expected, mapped);
        Assert.Equal(domainType, AIMapper.MapProviderType(mapped));
    }

    [Theory]
    [InlineData(DomainAI.InsightCategory.Revenue, InsightCategory.Revenue)]
    [InlineData(DomainAI.InsightCategory.Customer, InsightCategory.Customer)]
    [InlineData(DomainAI.InsightCategory.Appointment, InsightCategory.Appointment)]
    [InlineData(DomainAI.InsightCategory.Inventory, InsightCategory.Inventory)]
    [InlineData(DomainAI.InsightCategory.Hr, InsightCategory.Hr)]
    [InlineData(DomainAI.InsightCategory.Payroll, InsightCategory.Payroll)]
    [InlineData(DomainAI.InsightCategory.Attendance, InsightCategory.Attendance)]
    [InlineData(DomainAI.InsightCategory.Commission, InsightCategory.Commission)]
    [InlineData(DomainAI.InsightCategory.General, InsightCategory.General)]
    public void MapCategory_RoundTripsEveryValue(DomainAI.InsightCategory domainCategory, InsightCategory expected)
    {
        var mapped = AIMapper.MapCategory(domainCategory);
        Assert.Equal(expected, mapped);
        Assert.Equal(domainCategory, AIMapper.MapCategory(mapped));
    }

    [Theory]
    [InlineData(DomainAI.InsightSeverity.Info, InsightSeverity.Info)]
    [InlineData(DomainAI.InsightSeverity.Trend, InsightSeverity.Trend)]
    [InlineData(DomainAI.InsightSeverity.Opportunity, InsightSeverity.Opportunity)]
    [InlineData(DomainAI.InsightSeverity.Risk, InsightSeverity.Risk)]
    [InlineData(DomainAI.InsightSeverity.Critical, InsightSeverity.Critical)]
    public void MapSeverity_MapsEveryValue(DomainAI.InsightSeverity domainSeverity, InsightSeverity expected)
    {
        Assert.Equal(expected, AIMapper.MapSeverity(domainSeverity));
    }

    [Theory]
    [InlineData(DomainAI.RecommendationPriority.Low, RecommendationPriority.Low)]
    [InlineData(DomainAI.RecommendationPriority.Medium, RecommendationPriority.Medium)]
    [InlineData(DomainAI.RecommendationPriority.High, RecommendationPriority.High)]
    [InlineData(DomainAI.RecommendationPriority.Urgent, RecommendationPriority.Urgent)]
    public void MapPriority_MapsEveryValue(DomainAI.RecommendationPriority domainPriority, RecommendationPriority expected)
    {
        Assert.Equal(expected, AIMapper.MapPriority(domainPriority));
    }

    [Fact]
    public void MapSession_CopiesEveryField()
    {
        var now = DateTimeOffset.Now;
        var session = new DomainAI.ConversationSession("s1", "Title", now, now, true);

        var dto = AIMapper.MapSession(session);

        Assert.Equal("s1", dto.Id);
        Assert.Equal("Title", dto.Title);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(now, dto.UpdatedAt);
        Assert.True(dto.IsPinned);
    }

    [Fact]
    public void MapMessage_CopiesEveryFieldAndMapsRole()
    {
        var now = DateTimeOffset.Now;
        var message = new DomainAI.ConversationMessage("m1", "s1", DomainAI.ConversationRole.Assistant, "Hello", now, 5);

        var dto = AIMapper.MapMessage(message);

        Assert.Equal("m1", dto.Id);
        Assert.Equal("s1", dto.SessionId);
        Assert.Equal(ConversationRole.Assistant, dto.Role);
        Assert.Equal("Hello", dto.Content);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(5, dto.TokenCount);
    }

    [Fact]
    public void MapProviderConfiguration_RoundTrips()
    {
        var domainConfiguration = new DomainAI.AIProviderConfiguration(DomainAI.AIProviderType.Anthropic, "claude", true);

        var dto = AIMapper.MapProviderConfiguration(domainConfiguration);
        var roundTripped = AIMapper.MapProviderConfiguration(dto);

        Assert.Equal(AIProviderType.Anthropic, dto.ProviderType);
        Assert.Equal("claude", dto.ModelId);
        Assert.True(dto.IsEnabled);
        Assert.Equal(domainConfiguration, roundTripped);
    }

    [Fact]
    public void MapSettings_RoundTrips()
    {
        var domainSettings = new DomainAI.AISettings(true, false, true, false);

        var dto = AIMapper.MapSettings(domainSettings);
        var roundTripped = AIMapper.MapSettings(dto);

        Assert.Equal(domainSettings, roundTripped);
    }

    [Fact]
    public void MapTokenUsage_ComputesTotalTokens()
    {
        var now = DateTimeOffset.Now;
        var record = new DomainAI.TokenUsageRecord("u1", "s1", DomainAI.AIProviderType.Mock, 100, 50, now);

        var dto = AIMapper.MapTokenUsage(record);

        Assert.Equal(150, dto.TotalTokens);
    }

    [Fact]
    public void MapHealthScore_MapsEveryComponent()
    {
        var now = DateTimeOffset.Now;
        var score = new DomainAI.BusinessHealthScore(
            72.5m,
            [new DomainAI.BusinessHealthComponent(DomainAI.InsightCategory.Revenue, "Revenue", 80m, 0.5m)],
            "Solid.",
            now);

        var dto = AIMapper.MapHealthScore(score);

        Assert.Equal(72.5m, dto.OverallScore);
        Assert.Single(dto.Components);
        Assert.Equal(InsightCategory.Revenue, dto.Components[0].Category);
        Assert.Equal("Solid.", dto.Summary);
        Assert.Equal(now, dto.ComputedAt);
    }
}
