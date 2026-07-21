namespace Rojan.Desktop.Application.Automation;

/// <summary>Business Rules Engine CRUD plus evaluation/execution (Requirement 32.2) - "IF Customer is VIP → Apply Discount", "IF Inventory &lt; Threshold → Raise Notification", "IF Employee Absent &gt; 3 Days → Notify Manager".</summary>
public interface IBusinessRuleService
{
    public Task<IReadOnlyList<BusinessRuleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<BusinessRuleDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task<BusinessRuleDto> CreateAsync(
        string name,
        string description,
        IReadOnlyList<BusinessRuleConditionDto> conditions,
        BusinessRuleActionDto action,
        int priority,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default);

    public Task<BusinessRuleDto> UpdateAsync(BusinessRuleDto rule, CancellationToken cancellationToken = default);

    public Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every enabled rule that matches <paramref name="facts"/>, most-important first - a pure query, no actions performed. Used to preview what a fact bag would trigger.</summary>
    public Task<IReadOnlyList<BusinessRuleDto>> EvaluateAsync(IReadOnlyDictionary<string, string> facts, CancellationToken cancellationToken = default);

    /// <summary>Evaluates every rule against <paramref name="facts"/> and actually performs each match's action (raising a notification, triggering a workflow, ...) - see the class doc comment on <c>BusinessRuleService</c> for exactly what each <see cref="BusinessRuleActionType"/> does.</summary>
    public Task<IReadOnlyList<BusinessRuleDto>> ExecuteMatchingRulesAsync(
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default);
}
