using Rojan.Desktop.Application.Notifications;
using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>
/// Default <see cref="IBusinessRuleService"/>. Matching is pure Domain
/// logic (<c>DomainAutomation.BusinessRuleEngine</c>); performing a
/// matched rule's action is this service's own job, since it means
/// calling other services:
/// <see cref="DomainAutomation.BusinessRuleActionType.RaiseNotification"/>/
/// <see cref="DomainAutomation.BusinessRuleActionType.NotifyManager"/> call
/// the existing Phase 27 <see cref="INotificationService"/> (Requirement
/// 32.6's integration, not a modification);
/// <see cref="DomainAutomation.BusinessRuleActionType.TriggerWorkflow"/>
/// calls <see cref="IWorkflowExecutionEngine"/> directly with the
/// workflow id from the rule action's <c>Parameters["workflowId"]</c>;
/// <see cref="DomainAutomation.BusinessRuleActionType.ApplyDiscount"/>
/// raises a notification describing the discount rather than mutating a
/// real booking/invoice - actually applying one would mean touching the
/// Accounting/Booking modules, out of this phase's "do not modify
/// existing modules" scope, so it stays a documented, notify-only action
/// for now; <see cref="DomainAutomation.BusinessRuleActionType.Custom"/>
/// is a no-op extension point.
/// </summary>
public sealed class BusinessRuleService : IBusinessRuleService
{
    private readonly DomainAutomation.IBusinessRuleRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IWorkflowExecutionEngine _executionEngine;

    public BusinessRuleService(DomainAutomation.IBusinessRuleRepository repository, INotificationService notificationService, IWorkflowExecutionEngine executionEngine)
    {
        _repository = repository;
        _notificationService = notificationService;
        _executionEngine = executionEngine;
    }

    public async Task<IReadOnlyList<BusinessRuleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return rules.OrderBy(rule => rule.Priority).Select(AutomationMapping.Map).ToList();
    }

    public async Task<BusinessRuleDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return rule is null ? null : AutomationMapping.Map(rule);
    }

    public async Task<BusinessRuleDto> CreateAsync(
        string name,
        string description,
        IReadOnlyList<BusinessRuleConditionDto> conditions,
        BusinessRuleActionDto action,
        int priority,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var rule = new DomainAutomation.BusinessRule(
            Guid.NewGuid().ToString("N"), name, description, conditions.Select(AutomationMapping.MapToDomain).ToList(),
            AutomationMapping.MapToDomain(action), priority, IsEnabled: true, now, now, organizationId, branchId);

        await _repository.SaveAsync(rule, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(rule);
    }

    public async Task<BusinessRuleDto> UpdateAsync(BusinessRuleDto rule, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(rule.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Business rule '{rule.Id}' was not found.");

        var updated = existing with
        {
            Name = rule.Name,
            Description = rule.Description,
            Conditions = rule.Conditions.Select(AutomationMapping.MapToDomain).ToList(),
            Action = AutomationMapping.MapToDomain(rule.Action),
            Priority = rule.Priority,
            IsEnabled = rule.IsEnabled,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(updated);
    }

    public async Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Business rule '{id}' was not found.");

        await _repository.SaveAsync(rule with { IsEnabled = isEnabled, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public async Task<IReadOnlyList<BusinessRuleDto>> EvaluateAsync(IReadOnlyDictionary<string, string> facts, CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return DomainAutomation.BusinessRuleEngine.EvaluateAll(rules, facts).Select(AutomationMapping.Map).ToList();
    }

    public async Task<IReadOnlyList<BusinessRuleDto>> ExecuteMatchingRulesAsync(
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var matched = DomainAutomation.BusinessRuleEngine.EvaluateAll(rules, facts);

        foreach (var rule in matched)
        {
            await PerformActionAsync(rule, organizationId, branchId, triggeredByUserId, cancellationToken).ConfigureAwait(false);
        }

        return matched.Select(AutomationMapping.Map).ToList();
    }

    private async Task PerformActionAsync(DomainAutomation.BusinessRule rule, string organizationId, string branchId, string triggeredByUserId, CancellationToken cancellationToken)
    {
        switch (rule.Action.Type)
        {
            case DomainAutomation.BusinessRuleActionType.RaiseNotification:
                await _notificationService.RaiseAsync(
                    new NotificationRequest(NotificationSeverity.Information, NotificationPriority.Normal, "Automation_Rule_NotificationTitle", "Automation_Rule_NotificationMessage", [rule.Name], Category: "automation"),
                    cancellationToken).ConfigureAwait(false);
                break;

            case DomainAutomation.BusinessRuleActionType.NotifyManager:
                await _notificationService.RaiseAsync(
                    new NotificationRequest(NotificationSeverity.Warning, NotificationPriority.High, "Automation_Rule_NotifyManagerTitle", "Automation_Rule_NotifyManagerMessage", [rule.Name], Category: "automation"),
                    cancellationToken).ConfigureAwait(false);
                break;

            case DomainAutomation.BusinessRuleActionType.ApplyDiscount:
                var percentage = rule.Action.Parameters.TryGetValue("percentage", out var pct) ? pct : "0";
                await _notificationService.RaiseAsync(
                    new NotificationRequest(NotificationSeverity.Success, NotificationPriority.Normal, "Automation_Rule_DiscountTitle", "Automation_Rule_DiscountMessage", [rule.Name, percentage], Category: "automation"),
                    cancellationToken).ConfigureAwait(false);
                break;

            case DomainAutomation.BusinessRuleActionType.TriggerWorkflow:
                if (rule.Action.Parameters.TryGetValue("workflowId", out var workflowId) && !string.IsNullOrWhiteSpace(workflowId))
                {
                    await _executionEngine.ExecuteAsync(workflowId, null, triggeredByUserId, rule.Action.Parameters, organizationId, branchId, cancellationToken).ConfigureAwait(false);
                }

                break;

            case DomainAutomation.BusinessRuleActionType.Custom:
            default:
                break;
        }
    }
}
