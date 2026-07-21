using System.Globalization;
using System.Windows.Input;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>
/// Phase 32: Enterprise Automation, Workflow &amp; Business Rules Engine -
/// the page behind the "Automation" module, one tab per requirement group
/// (Dashboard/Workflows/Business Rules/Scheduled Jobs/Approvals), the same
/// "one page, several child ViewModels" shape
/// <c>Reporting.ReportingPageViewModel</c>/<c>Analytics.AnalyticsPageViewModel</c>
/// already establish for a multi-section module. Derives the
/// organization/branch/user scope every child tab needs once, from the
/// existing <see cref="ICurrentSessionService"/> (an integration, not a
/// modification, per this phase's own "do not modify existing modules"
/// scope).
/// </summary>
public sealed class AutomationPageViewModel : ViewModelBase
{
    private int _selectedTabIndex;

    public AutomationPageViewModel(
        ICurrentSessionService currentSessionService,
        IAutomationDashboardQueryService dashboardQueryService,
        IWorkflowService workflowService,
        IBusinessRuleService businessRuleService,
        IScheduledJobService scheduledJobService,
        IApprovalService approvalService,
        IWorkflowExecutionEngine executionEngine)
    {
        var organizationId = currentSessionService.CurrentOrganization?.Id ?? string.Empty;
        var branchId = currentSessionService.CurrentBranch?.Id ?? string.Empty;
        var currentUserId = currentSessionService.CurrentRole.ToString();

        Dashboard = new AutomationDashboardTabViewModel(dashboardQueryService, executionEngine);
        Workflows = new WorkflowsTabViewModel(workflowService, executionEngine, currentUserId, organizationId, branchId);
        BusinessRules = new BusinessRulesTabViewModel(businessRuleService, organizationId, branchId);
        ScheduledJobs = new ScheduledJobsTabViewModel(scheduledJobService, workflowService, organizationId, branchId);
        Approvals = new ApprovalsTabViewModel(approvalService, currentUserId);

        SelectTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                SelectedTabIndex = index;
            }
        });

        _ = Dashboard.LoadAsync();
        _ = Workflows.LoadAsync();
        _ = BusinessRules.LoadAsync();
        _ = ScheduledJobs.LoadAsync();
        _ = Approvals.LoadAsync();
    }

    public AutomationDashboardTabViewModel Dashboard { get; }

    public WorkflowsTabViewModel Workflows { get; }

    public BusinessRulesTabViewModel BusinessRules { get; }

    public ScheduledJobsTabViewModel ScheduledJobs { get; }

    public ApprovalsTabViewModel Approvals { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public ICommand SelectTabCommand { get; }
}
