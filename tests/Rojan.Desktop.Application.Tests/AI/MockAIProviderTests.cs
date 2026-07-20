using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Application.AI.Providers;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class MockAIProviderTests
{
    private readonly MockAIProvider _sut = new();

    [Fact]
    public void ProviderType_IsMock()
    {
        Assert.Equal(AIProviderType.Mock, _sut.ProviderType);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsANonEmptyDeterministicReply()
    {
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "How is revenue?")], "mock-v1");

        var response = await _sut.CompleteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.Contains("How is revenue?", response.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_IsDeterministicForTheSameRequest()
    {
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "How is revenue?")], "mock-v1");

        var first = await _sut.CompleteAsync(request);
        var second = await _sut.CompleteAsync(request);

        Assert.Equal(first.Content, second.Content);
    }

    [Fact]
    public async Task CompleteAsync_EstimatesTokensFromCharacterCount()
    {
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "Hi")], "mock-v1");

        var response = await _sut.CompleteAsync(request);

        Assert.True(response.PromptTokens >= 1);
        Assert.True(response.CompletionTokens >= 1);
    }

    [Fact]
    public async Task StreamCompleteAsync_YieldsTheSameReplyAsCompleteAsyncWhenConcatenated()
    {
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "How is revenue?")], "mock-v1");
        var completed = await _sut.CompleteAsync(request);

        var builder = new System.Text.StringBuilder();
        await foreach (var chunk in _sut.StreamCompleteAsync(request))
        {
            builder.Append(chunk);
        }

        Assert.Equal(completed.Content, builder.ToString());
    }

    [Fact]
    public async Task StreamCompleteAsync_YieldsMoreThanOneChunkForAMultiWordReply()
    {
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "How is revenue?")], "mock-v1");

        var chunkCount = 0;
        await foreach (var _ in _sut.StreamCompleteAsync(request))
        {
            chunkCount++;
        }

        Assert.True(chunkCount > 1);
    }

    [Fact]
    public async Task StreamCompleteAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        var request = new AIProviderRequestDto("s1", [new AIProviderMessageDto(ConversationRole.User, "How is revenue this month across every service?")], "mock-v1");

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _sut.StreamCompleteAsync(request, cts.Token))
            {
                cts.Cancel();
            }
        });
    }
}
