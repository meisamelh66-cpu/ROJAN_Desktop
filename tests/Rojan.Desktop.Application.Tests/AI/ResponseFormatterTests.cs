using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class ResponseFormatterTests
{
    private readonly ResponseFormatter _sut = new();

    [Fact]
    public void Format_TrimsSurroundingWhitespace()
    {
        Assert.Equal("Hello", _sut.Format("   Hello   "));
    }

    [Fact]
    public void Format_CollapsesThreeOrMoreBlankLinesToTwo()
    {
        var result = _sut.Format("Line one\n\n\n\nLine two");

        Assert.Equal("Line one\n\nLine two", result);
    }

    [Fact]
    public void Format_WithEmptyOrWhitespaceInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, _sut.Format("   "));
        Assert.Equal(string.Empty, _sut.Format(string.Empty));
    }

    [Fact]
    public void Format_TruncatesResponsesLongerThanFourThousandCharacters()
    {
        var longResponse = new string('a', 5000);

        var result = _sut.Format(longResponse);

        Assert.EndsWith("...", result, StringComparison.Ordinal);
        Assert.Equal(4003, result.Length);
    }

    [Fact]
    public void Format_LeavesShortSingleBlankLinesAlone()
    {
        Assert.Equal("Line one\n\nLine two", _sut.Format("Line one\n\nLine two"));
    }
}
