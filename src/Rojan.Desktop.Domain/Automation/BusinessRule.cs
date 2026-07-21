namespace Rojan.Desktop.Domain.Automation;

/// <summary>How a <see cref="BusinessRuleCondition"/> compares its <see cref="BusinessRuleCondition.Field"/>'s runtime value against <see cref="BusinessRuleCondition.Value"/> - see <see cref="BusinessRuleEngine"/>.</summary>
public enum BusinessRuleOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
}

/// <summary>What a matched <see cref="BusinessRule"/> does - a small, extensible set (Requirement 32.2's "Rules must be extensible") rather than free-form code, so a rule stays data, never a security/trust boundary.</summary>
public enum BusinessRuleActionType
{
    RaiseNotification,
    ApplyDiscount,
    NotifyManager,
    TriggerWorkflow,
    Custom,
}

/// <summary>One AND-combined comparison a <see cref="BusinessRule"/> tests against a caller-supplied fact bag (e.g. <c>Field="CustomerStatus"</c>, <c>Operator=Equals</c>, <c>Value="Vip"</c>).</summary>
public sealed record BusinessRuleCondition(string Field, BusinessRuleOperator Operator, string Value);

/// <summary>What happens when every one of a <see cref="BusinessRule"/>'s <see cref="BusinessRule.Conditions"/> matches. <see cref="Parameters"/> is action-specific (e.g. ApplyDiscount's "percentage", TriggerWorkflow's "workflowId").</summary>
public sealed record BusinessRuleAction(BusinessRuleActionType Type, IReadOnlyDictionary<string, string> Parameters);

/// <summary>
/// A configurable, extensible IF/THEN rule ("IF Customer is VIP → Apply
/// Discount", "IF Inventory &lt; Threshold → Raise Notification", "IF
/// Employee Absent &gt; 3 Days → Notify Manager" - Requirement 32.2's own
/// examples), as returned by <see cref="IBusinessRuleRepository"/>. Every
/// <see cref="Conditions"/> entry must match (AND) for
/// <see cref="Action"/> to fire - see <see cref="BusinessRuleEngine.Evaluate"/>.
/// <see cref="Priority"/> (lower runs first) lets multiple matching rules
/// order deterministically when a caller evaluates several against the
/// same fact bag.
/// </summary>
public sealed record BusinessRule(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<BusinessRuleCondition> Conditions,
    BusinessRuleAction Action,
    int Priority,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string OrganizationId,
    string BranchId);
