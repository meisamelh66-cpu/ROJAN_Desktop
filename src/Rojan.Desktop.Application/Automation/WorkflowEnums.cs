namespace Rojan.Desktop.Application.Automation;

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowStepType</c> - the same "Application owns a parallel copy so Presentation never needs a Domain reference" pattern <c>Notifications.NotificationSeverity</c>/<c>Workspaces.PaneOrientation</c> already establish, enforced by the unchanged <c>ArchitectureTests.DependencyDirectionTests</c>.</summary>
public enum WorkflowStepType
{
    Start,
    End,
    Decision,
    Delay,
    Approval,
    Condition,
    Notification,
    Email,
    AiAction,
    DatabaseAction,
    ApiAction,
}

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowStatus</c> - see <see cref="WorkflowStepType"/>'s doc comment for why.</summary>
public enum WorkflowStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>Application's own mirror of <c>Domain.Automation.TriggerType</c> - see <see cref="WorkflowStepType"/>'s doc comment for why.</summary>
public enum TriggerType
{
    AppointmentCreated,
    AppointmentCancelled,
    CustomerRegistered,
    PaymentCompleted,
    LowInventory,
    EmployeeCreated,
    BranchCreated,
    LicenseExpired,
    Login,
    Logout,
}

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowExecutionStatus</c> - see <see cref="WorkflowStepType"/>'s doc comment for why.</summary>
public enum WorkflowExecutionStatus
{
    Running,
    Waiting,
    Completed,
    Failed,
    Cancelled,
}

/// <summary>Application's own mirror of <c>Domain.Automation.StepExecutionStatus</c> - see <see cref="WorkflowStepType"/>'s doc comment for why.</summary>
public enum StepExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
}
