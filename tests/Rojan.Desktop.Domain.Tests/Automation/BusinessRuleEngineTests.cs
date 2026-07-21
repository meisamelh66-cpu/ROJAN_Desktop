using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="BusinessRuleEngine"/> - condition matching and rule-set evaluation ("IF Customer is VIP", "IF Inventory &lt; Threshold", ...).</summary>
public sealed class BusinessRuleEngineTests
{
    private static BusinessRule Rule(string id, BusinessRuleCondition condition, int priority = 0, bool isEnabled = true) =>
        new(id, id, string.Empty, [condition], new BusinessRuleAction(BusinessRuleActionType.RaiseNotification, new Dictionary<string, string>()), priority, isEnabled, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "org-1", "branch-1");

    [Fact]
    public void EvaluateCondition_Equals_CaseInsensitiveMatch()
    {
        var condition = new BusinessRuleCondition("CustomerStatus", BusinessRuleOperator.Equals, "vip");
        var facts = new Dictionary<string, string> { ["CustomerStatus"] = "VIP" };

        Assert.True(BusinessRuleEngine.EvaluateCondition(condition, facts));
    }

    [Fact]
    public void EvaluateCondition_MissingField_ReturnsFalse()
    {
        var condition = new BusinessRuleCondition("Missing", BusinessRuleOperator.Equals, "x");

        Assert.False(BusinessRuleEngine.EvaluateCondition(condition, new Dictionary<string, string>()));
    }

    [Theory]
    [InlineData(BusinessRuleOperator.GreaterThan, "5", "3", true)]
    [InlineData(BusinessRuleOperator.GreaterThan, "3", "5", false)]
    [InlineData(BusinessRuleOperator.LessThan, "3", "5", true)]
    [InlineData(BusinessRuleOperator.GreaterThanOrEqual, "5", "5", true)]
    [InlineData(BusinessRuleOperator.LessThanOrEqual, "5", "5", true)]
    public void EvaluateCondition_NumericOperators_CompareAsNumbers(BusinessRuleOperator op, string actual, string expected, bool result)
    {
        var condition = new BusinessRuleCondition("Inventory", op, expected);
        var facts = new Dictionary<string, string> { ["Inventory"] = actual };

        Assert.Equal(result, BusinessRuleEngine.EvaluateCondition(condition, facts));
    }

    [Fact]
    public void EvaluateCondition_NumericOperatorWithNonNumericValue_ReturnsFalseRatherThanThrowing()
    {
        var condition = new BusinessRuleCondition("Field", BusinessRuleOperator.GreaterThan, "not-a-number");
        var facts = new Dictionary<string, string> { ["Field"] = "also-not-a-number" };

        Assert.False(BusinessRuleEngine.EvaluateCondition(condition, facts));
    }

    [Fact]
    public void EvaluateCondition_Contains_SubstringMatchCaseInsensitive()
    {
        var condition = new BusinessRuleCondition("Notes", BusinessRuleOperator.Contains, "urgent");
        var facts = new Dictionary<string, string> { ["Notes"] = "This is URGENT." };

        Assert.True(BusinessRuleEngine.EvaluateCondition(condition, facts));
    }

    [Fact]
    public void EvaluateCondition_NotEquals_ReturnsTrueWhenDifferent()
    {
        var condition = new BusinessRuleCondition("Status", BusinessRuleOperator.NotEquals, "Closed");
        var facts = new Dictionary<string, string> { ["Status"] = "Open" };

        Assert.True(BusinessRuleEngine.EvaluateCondition(condition, facts));
    }

    [Fact]
    public void Evaluate_AllConditionsMustMatch()
    {
        var rule = new BusinessRule(
            "r1", "Multi", string.Empty,
            [
                new BusinessRuleCondition("A", BusinessRuleOperator.Equals, "1"),
                new BusinessRuleCondition("B", BusinessRuleOperator.Equals, "2"),
            ],
            new BusinessRuleAction(BusinessRuleActionType.RaiseNotification, new Dictionary<string, string>()),
            0, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "org-1", "branch-1");

        Assert.False(BusinessRuleEngine.Evaluate(rule, new Dictionary<string, string> { ["A"] = "1", ["B"] = "wrong" }));
        Assert.True(BusinessRuleEngine.Evaluate(rule, new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }));
    }

    [Fact]
    public void EvaluateAll_ExcludesDisabledRules()
    {
        var condition = new BusinessRuleCondition("X", BusinessRuleOperator.Equals, "1");
        var rules = new[] { Rule("r1", condition, isEnabled: false) };

        var matched = BusinessRuleEngine.EvaluateAll(rules, new Dictionary<string, string> { ["X"] = "1" });

        Assert.Empty(matched);
    }

    [Fact]
    public void EvaluateAll_OrdersByPriorityAscending()
    {
        var condition = new BusinessRuleCondition("X", BusinessRuleOperator.Equals, "1");
        var rules = new[] { Rule("low", condition, priority: 5), Rule("high", condition, priority: 1) };

        var matched = BusinessRuleEngine.EvaluateAll(rules, new Dictionary<string, string> { ["X"] = "1" });

        Assert.Equal(["high", "low"], matched.Select(rule => rule.Id));
    }
}
