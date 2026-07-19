using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class MoneyParserTests
{
    [Theory]
    [InlineData("$65", 65)]
    [InlineData("$1,250", 1250)]
    [InlineData("$1,250.50", 1250.50)]
    [InlineData("65", 65)]
    [InlineData("$65 ", 65)]
    public void Parse_WithValidMoneyString_ReturnsCorrectDecimal(string value, decimal expected)
    {
        Assert.Equal(expected, MoneyParser.Parse(value));
    }

    [Fact]
    public void Parse_WithUnparsableString_ReturnsZero()
    {
        Assert.Equal(0m, MoneyParser.Parse("not a number"));
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsZero()
    {
        Assert.Equal(0m, MoneyParser.Parse(string.Empty));
    }
}
