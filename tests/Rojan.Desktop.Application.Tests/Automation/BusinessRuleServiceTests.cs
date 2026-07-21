using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="BusinessRuleService"/> - Requirement 32.2's configurable rules plus the action side-effects (notification/manager/discount/trigger-workflow) 32.6's automation integrations require.</summary>
public sealed class BusinessRuleServiceTests
{
    private static BusinessRuleConditionDto VipCondition() => new("IsVip", BusinessRuleOperator.Equals, "true");

    [Fact]
    public async Task CreateAsync_PersistsANewEnabledRule()
    {
        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new BusinessRuleService(repository, notifications, engine);

        var rule = await service.CreateAsync("VIP Discount", "", [VipCondition()], new BusinessRuleActionDto(BusinessRuleActionType.ApplyDiscount, new Dictionary<string, string> { ["percentage"] = "10" }), priority: 1, "org-1", "branch-1");

        Assert.True(rule.IsEnabled);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsOnlyRulesWhoseConditionsMatchTheGivenFacts()
    {
        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new BusinessRuleService(repository, notifications, engine);
        await service.CreateAsync("VIP Discount", "", [VipCondition()], new BusinessRuleActionDto(BusinessRuleActionType.ApplyDiscount, new Dictionary<string, string>()), 1, "org-1", "branch-1");

        var matchingVip = await service.EvaluateAsync(new Dictionary<string, string> { ["IsVip"] = "true" });
        var nonVip = await service.EvaluateAsync(new Dictionary<string, string> { ["IsVip"] = "false" });

        Assert.Single(matchingVip);
        Assert.Empty(nonVip);
    }

    [Fact]
    public async Task ExecuteMatchingRulesAsync_RaiseNotificationAction_CallsNotificationService()
    {
        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new BusinessRuleService(repository, notifications, engine);
        await service.CreateAsync("Low Stock", "", [new BusinessRuleConditionDto("Stock", BusinessRuleOperator.LessThan, "5")], new BusinessRuleActionDto(BusinessRuleActionType.RaiseNotification, new Dictionary<string, string>()), 1, "org-1", "branch-1");

        var matched = await service.ExecuteMatchingRulesAsync(new Dictionary<string, string> { ["Stock"] = "2" }, "org-1", "branch-1", "user-1");

        Assert.Single(matched);
        Assert.Single(notifications.RaisedRequests);
    }

    [Fact]
    public async Task ExecuteMatchingRulesAsync_TriggerWorkflowAction_ExecutesTheReferencedWorkflow()
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var workflowService = new WorkflowService(workflows);
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var draft = await workflowService.CreateDraftAsync("Notify Manager Flow", "", [AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId), AutomationTestFactory.Step(endId, WorkflowStepType.End)], [], "user-1", "org-1", "branch-1");
        var published = await workflowService.PublishAsync(draft.Id);

        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        var service = new BusinessRuleService(repository, notifications, engine);
        await service.CreateAsync("Absence Rule", "", [new BusinessRuleConditionDto("DaysAbsent", BusinessRuleOperator.GreaterThan, "3")], new BusinessRuleActionDto(BusinessRuleActionType.TriggerWorkflow, new Dictionary<string, string> { ["workflowId"] = published.Id }), 1, "org-1", "branch-1");

        await service.ExecuteMatchingRulesAsync(new Dictionary<string, string> { ["DaysAbsent"] = "5" }, "org-1", "branch-1", "user-1");

        Assert.Single(await executions.GetAllAsync());
    }

    [Fact]
    public async Task SetEnabledAsync_DisabledRule_NoLongerMatches()
    {
        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new BusinessRuleService(repository, notifications, engine);
        var rule = await service.CreateAsync("VIP Discount", "", [VipCondition()], new BusinessRuleActionDto(BusinessRuleActionType.ApplyDiscount, new Dictionary<string, string>()), 1, "org-1", "branch-1");

        await service.SetEnabledAsync(rule.Id, false);
        var matching = await service.EvaluateAsync(new Dictionary<string, string> { ["IsVip"] = "true" });

        Assert.Empty(matching);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRule()
    {
        var repository = new FakeBusinessRuleRepository();
        var notifications = new StubNotificationService();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new BusinessRuleService(repository, notifications, engine);
        var rule = await service.CreateAsync("VIP Discount", "", [VipCondition()], new BusinessRuleActionDto(BusinessRuleActionType.ApplyDiscount, new Dictionary<string, string>()), 1, "org-1", "branch-1");

        await service.DeleteAsync(rule.Id);

        Assert.Null(await service.GetByIdAsync(rule.Id));
    }
}
