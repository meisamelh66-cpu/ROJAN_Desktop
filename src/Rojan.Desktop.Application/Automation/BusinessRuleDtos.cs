namespace Rojan.Desktop.Application.Automation;

/// <summary>Application's own mirror of <c>Domain.Automation.BusinessRuleOperator</c>.</summary>
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

/// <summary>Application's own mirror of <c>Domain.Automation.BusinessRuleActionType</c>.</summary>
public enum BusinessRuleActionType
{
    RaiseNotification,
    ApplyDiscount,
    NotifyManager,
    TriggerWorkflow,
    Custom,
}

/// <summary>Application's own mirror of <c>Domain.Automation.BusinessRuleCondition</c>.</summary>
public sealed record BusinessRuleConditionDto(string Field, BusinessRuleOperator Operator, string Value);

/// <summary>Application's own mirror of <c>Domain.Automation.BusinessRuleAction</c>.</summary>
public sealed record BusinessRuleActionDto(BusinessRuleActionType Type, IReadOnlyDictionary<string, string> Parameters);

/// <summary>Application's own mirror of <c>Domain.Automation.BusinessRule</c>.</summary>
public sealed record BusinessRuleDto(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<BusinessRuleConditionDto> Conditions,
    BusinessRuleActionDto Action,
    int Priority,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string OrganizationId,
    string BranchId);
