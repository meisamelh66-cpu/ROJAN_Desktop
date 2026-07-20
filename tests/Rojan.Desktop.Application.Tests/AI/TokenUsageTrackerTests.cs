using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class TokenUsageTrackerTests
{
    private static TokenUsageTracker CreateSut() => new(new StubAIRepository());

    [Fact]
    public async Task RecordAsync_ComputesTotalTokens()
    {
        var sut = CreateSut();

        var record = await sut.RecordAsync("s1", AIProviderType.Mock, 100, 50);

        Assert.Equal(150, record.TotalTokens);
    }

    [Fact]
    public async Task GetUsageHistoryAsync_ReturnsEveryRecordedUsage()
    {
        var sut = CreateSut();
        await sut.RecordAsync("s1", AIProviderType.Mock, 10, 5);
        await sut.RecordAsync("s1", AIProviderType.Mock, 20, 10);

        var history = await sut.GetUsageHistoryAsync();

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task GetTotalTokensAsync_SumsAcrossEveryRecord()
    {
        var sut = CreateSut();
        await sut.RecordAsync("s1", AIProviderType.Mock, 10, 5);
        await sut.RecordAsync("s1", AIProviderType.Mock, 20, 10);

        var total = await sut.GetTotalTokensAsync();

        Assert.Equal(45, total);
    }
}
