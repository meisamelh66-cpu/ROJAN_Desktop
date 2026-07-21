namespace Rojan.Desktop.Domain.Automation;

/// <summary>One node type in a <see cref="WorkflowDefinition"/>'s step graph. AiAction/DatabaseAction/ApiAction are architecture/contract-only in this phase - see <c>Application.Automation.WorkflowStepExecutors</c> for the documented "no external calls yet" boundary.</summary>
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

/// <summary>A <see cref="WorkflowDefinition"/>'s lifecycle stage - see <c>WorkflowRules</c> for legal transitions.</summary>
public enum WorkflowStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>A business event the Trigger Engine (<c>Application.Automation.ITriggerEngine</c>) can dispatch to every enabled <see cref="WorkflowStatus.Published"/> workflow subscribed to it.</summary>
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

/// <summary>A single <see cref="WorkflowExecution"/>'s overall state.</summary>
public enum WorkflowExecutionStatus
{
    Running,
    Waiting,
    Completed,
    Failed,
    Cancelled,
}

/// <summary>A single step's state within one <see cref="WorkflowExecution"/> - see <see cref="WorkflowStepExecutionLog"/>.</summary>
public enum StepExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
}
