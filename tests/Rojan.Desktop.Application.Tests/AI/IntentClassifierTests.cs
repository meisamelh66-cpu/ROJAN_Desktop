using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class IntentClassifierTests
{
    private readonly IntentClassifier _sut = new();

    [Theory]
    [InlineData("How is revenue trending?", InsightCategory.Revenue)]
    [InlineData("Tell me about our customer retention", InsightCategory.Customer)]
    [InlineData("What's on the appointment schedule today?", InsightCategory.Appointment)]
    [InlineData("Which products are low on stock?", InsightCategory.Inventory)]
    [InlineData("How is the staff doing?", InsightCategory.Hr)]
    [InlineData("What is this month's payroll?", InsightCategory.Payroll)]
    [InlineData("Was anyone late today?", InsightCategory.Attendance)]
    [InlineData("How much commission did specialists earn?", InsightCategory.Commission)]
    public void ClassifyIntent_MatchesKeywordsToTheirCategory(string message, InsightCategory expected)
    {
        Assert.Equal(expected, _sut.ClassifyIntent(message));
    }

    [Fact]
    public void ClassifyIntent_WithNoKeywordMatch_ReturnsGeneral()
    {
        Assert.Equal(InsightCategory.General, _sut.ClassifyIntent("What's the weather like today?"));
    }

    [Fact]
    public void ClassifyIntent_WithEmptyMessage_ReturnsGeneral()
    {
        Assert.Equal(InsightCategory.General, _sut.ClassifyIntent(string.Empty));
    }
}
