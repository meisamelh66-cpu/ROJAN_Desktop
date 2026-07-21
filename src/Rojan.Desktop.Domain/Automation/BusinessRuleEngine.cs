using System.Globalization;

namespace Rojan.Desktop.Domain.Automation;

/// <summary>
/// Pure condition-matching logic for <see cref="BusinessRule"/>s against a
/// caller-supplied, string-keyed fact bag - no I/O, no knowledge of what a
/// matched rule's action actually does (that's
/// <c>Application.Automation.BusinessRuleService</c>'s job, since raising a
/// notification/applying a discount means calling other services). Numeric
/// operators (<see cref="BusinessRuleOperator.GreaterThan"/> etc.) parse
/// both sides as <see cref="double"/> via <see cref="CultureInfo.InvariantCulture"/>
/// and fall back to <see langword="false"/> (never throw) if either side
/// isn't numeric - a misconfigured rule simply never matches rather than
/// crashing whatever raised the fact bag.
/// </summary>
public static class BusinessRuleEngine
{
    /// <summary>Whether every one of <paramref name="rule"/>'s <see cref="BusinessRule.Conditions"/> matches <paramref name="facts"/> (AND) - an empty condition list always matches.</summary>
    public static bool Evaluate(BusinessRule rule, IReadOnlyDictionary<string, string> facts) =>
        rule.Conditions.All(condition => EvaluateCondition(condition, facts));

    public static bool EvaluateCondition(BusinessRuleCondition condition, IReadOnlyDictionary<string, string> facts)
    {
        if (!facts.TryGetValue(condition.Field, out var actual))
        {
            return false;
        }

        return condition.Operator switch
        {
            BusinessRuleOperator.Equals => string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
            BusinessRuleOperator.NotEquals => !string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
            BusinessRuleOperator.Contains => actual.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            BusinessRuleOperator.GreaterThan => CompareNumeric(actual, condition.Value, (a, b) => a > b),
            BusinessRuleOperator.GreaterThanOrEqual => CompareNumeric(actual, condition.Value, (a, b) => a >= b),
            BusinessRuleOperator.LessThan => CompareNumeric(actual, condition.Value, (a, b) => a < b),
            BusinessRuleOperator.LessThanOrEqual => CompareNumeric(actual, condition.Value, (a, b) => a <= b),
            _ => false,
        };
    }

    /// <summary>Every rule in <paramref name="rules"/> that matches <paramref name="facts"/>, most-important (lowest <see cref="BusinessRule.Priority"/>) first.</summary>
    public static IReadOnlyList<BusinessRule> EvaluateAll(IEnumerable<BusinessRule> rules, IReadOnlyDictionary<string, string> facts) =>
        rules
            .Where(rule => rule.IsEnabled && Evaluate(rule, facts))
            .OrderBy(rule => rule.Priority)
            .ToList();

    private static bool CompareNumeric(string actual, string expected, Func<double, double, bool> compare) =>
        double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var actualNumber)
        && double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber)
        && compare(actualNumber, expectedNumber);
}
