using Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Domain.Tests.AI;

public sealed class ConversationRulesTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    public void CanPin_EnforcesMaxPinnedSessionsCap(int currentPinnedCount, bool expected)
    {
        Assert.Equal(expected, ConversationRules.CanPin(currentPinnedCount));
    }

    [Fact]
    public void DeriveTitle_WhenShorterThanLimit_ReturnsTrimmedMessage()
    {
        Assert.Equal("How is revenue trending?", ConversationRules.DeriveTitle("  How is revenue trending?  "));
    }

    [Fact]
    public void DeriveTitle_WhenLongerThanLimit_TruncatesWithEllipsis()
    {
        var longMessage = new string('a', 80);

        var title = ConversationRules.DeriveTitle(longMessage);

        Assert.Equal(63, title.Length);
        Assert.EndsWith("...", title, StringComparison.Ordinal);
        Assert.Equal(new string('a', 60), title[..60]);
    }

    [Fact]
    public void DeriveTitle_WhenExactlyAtLimit_DoesNotTruncate()
    {
        var message = new string('a', 60);

        Assert.Equal(message, ConversationRules.DeriveTitle(message));
    }
}
