using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Support;
using Rojan.Desktop.Application.Workspaces;
using AppServices = Rojan.Desktop.Application.Services;
using AiProviders = Rojan.Desktop.Application.AI.Providers;

namespace Rojan.Desktop.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers. The
/// Services vertical slice is aliased (<c>AppServices</c>) to avoid any
/// visual confusion with <see cref="IServiceCollection"/>/
/// <see cref="ServiceCollectionExtensions"/> in this same file - same
/// names, unrelated concepts.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardQueryService, DashboardQueryService>();
        services.AddSingleton<ICustomerQueryService, CustomerQueryService>();
        services.AddSingleton<ICustomerProfileQueryService, CustomerProfileQueryService>();

        // Phase 22A: the raw command service is registered as itself, then
        // wrapped by a permission-enforcing decorator registered as the
        // public interface - see CustomerCommandServicePermissionGate's own
        // doc comment. Every other module's *CommandService below follows
        // this same pair.
        //
        // Sprint 7 Commit 3: Customer/Specialist additionally get a sync
        // producer decorator threaded between the raw service and the
        // permission gate - see CustomerCommandServiceSyncProducer's own
        // doc comment for why this ordering (permission check stays
        // outermost, sync production only ever sees a call that already
        // passed it).
        services.AddSingleton<CustomerCommandService>();
        services.AddSingleton<CustomerCommandServiceSyncProducer>(sp =>
            new CustomerCommandServiceSyncProducer(sp.GetRequiredService<CustomerCommandService>(), sp.GetRequiredService<ISyncQueueService>()));
        services.AddSingleton<ICustomerCommandService>(sp =>
            new CustomerCommandServicePermissionGate(sp.GetRequiredService<CustomerCommandServiceSyncProducer>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IBookingQueryService, BookingQueryService>();
        services.AddSingleton<BookingCommandService>();
        services.AddSingleton<IBookingCommandService>(sp =>
            new BookingCommandServicePermissionGate(sp.GetRequiredService<BookingCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<ISpecialistQueryService, SpecialistQueryService>();
        services.AddSingleton<ISpecialistProfileQueryService, SpecialistProfileQueryService>();
        services.AddSingleton<SpecialistCommandService>();
        services.AddSingleton<SpecialistCommandServiceSyncProducer>(sp =>
            new SpecialistCommandServiceSyncProducer(sp.GetRequiredService<SpecialistCommandService>(), sp.GetRequiredService<ISyncQueueService>()));
        services.AddSingleton<ISpecialistCommandService>(sp =>
            new SpecialistCommandServicePermissionGate(sp.GetRequiredService<SpecialistCommandServiceSyncProducer>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<AppServices.IServiceQueryService, AppServices.ServiceQueryService>();
        services.AddSingleton<AppServices.IServiceProfileQueryService, AppServices.ServiceProfileQueryService>();
        services.AddSingleton<AppServices.ServiceCommandService>();
        services.AddSingleton<AppServices.IServiceCommandService>(sp =>
            new AppServices.ServiceCommandServicePermissionGate(sp.GetRequiredService<AppServices.ServiceCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IIntelligenceEngine, IntelligenceEngine>();
        services.AddSingleton<ICalendarQueryService, CalendarQueryService>();
        services.AddSingleton<CalendarCommandService>();
        services.AddSingleton<ICalendarCommandService>(sp =>
            new CalendarCommandServicePermissionGate(sp.GetRequiredService<CalendarCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<BookingWorkflowService>();
        services.AddSingleton<IBookingWorkflowService>(sp =>
            new BookingWorkflowServicePermissionGate(sp.GetRequiredService<BookingWorkflowService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IProductQueryService, ProductQueryService>();
        services.AddSingleton<IProductProfileQueryService, ProductProfileQueryService>();
        services.AddSingleton<IInventoryQueryService, InventoryQueryService>();
        services.AddSingleton<InventoryCommandService>();
        services.AddSingleton<IInventoryCommandService>(sp =>
            new InventoryCommandServicePermissionGate(sp.GetRequiredService<InventoryCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IInvoiceQueryService, InvoiceQueryService>();
        services.AddSingleton<InvoiceCommandService>();
        services.AddSingleton<IInvoiceCommandService>(sp =>
            new InvoiceCommandServicePermissionGate(sp.GetRequiredService<InvoiceCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IPaymentQueryService, PaymentQueryService>();
        services.AddSingleton<PaymentCommandService>();
        services.AddSingleton<IPaymentCommandService>(sp =>
            new PaymentCommandServicePermissionGate(sp.GetRequiredService<PaymentCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IEmployeeQueryService, EmployeeQueryService>();
        services.AddSingleton<EmployeeCommandService>();
        services.AddSingleton<IEmployeeCommandService>(sp =>
            new EmployeeCommandServicePermissionGate(sp.GetRequiredService<EmployeeCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IAttendanceQueryService, AttendanceQueryService>();
        services.AddSingleton<AttendanceCommandService>();
        services.AddSingleton<IAttendanceCommandService>(sp =>
            new AttendanceCommandServicePermissionGate(sp.GetRequiredService<AttendanceCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IShiftQueryService, ShiftQueryService>();
        services.AddSingleton<ShiftCommandService>();
        services.AddSingleton<IShiftCommandService>(sp =>
            new ShiftCommandServicePermissionGate(sp.GetRequiredService<ShiftCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<ICommissionQueryService, CommissionQueryService>();
        services.AddSingleton<CommissionCommandService>();
        services.AddSingleton<ICommissionCommandService>(sp =>
            new CommissionCommandServicePermissionGate(sp.GetRequiredService<CommissionCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IPayrollQueryService, PayrollQueryService>();
        services.AddSingleton<PayrollCommandService>();
        services.AddSingleton<IPayrollCommandService>(sp =>
            new PayrollCommandServicePermissionGate(sp.GetRequiredService<PayrollCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IReportCatalogQueryService, ReportCatalogQueryService>();
        services.AddSingleton<IReportExecutionQueryService, ReportExecutionQueryService>();
        services.AddSingleton<IReportSnapshotQueryService, ReportSnapshotQueryService>();
        services.AddSingleton<ReportSnapshotCommandService>();
        services.AddSingleton<IReportSnapshotCommandService>(sp =>
            new ReportSnapshotCommandServicePermissionGate(sp.GetRequiredService<ReportSnapshotCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IKpiEngineQueryService, KpiEngineQueryService>();
        services.AddSingleton<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddSingleton<ReportExportService>();
        services.AddSingleton<IReportExportService>(sp =>
            new ReportExportServicePermissionGate(sp.GetRequiredService<ReportExportService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IPromptTemplateRepository, PromptTemplateRepository>();
        services.AddSingleton<ConversationManager>();
        services.AddSingleton<IConversationManager>(sp =>
            new ConversationManagerPermissionGate(sp.GetRequiredService<ConversationManager>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IAIHistoryService, AIHistoryService>();
        services.AddSingleton<ITokenUsageTracker, TokenUsageTracker>();
        services.AddSingleton<IAIConfigurationService, AIConfigurationService>();
        services.AddSingleton<IAISettingsService, AISettingsService>();
        services.AddSingleton<IIntentClassifier, IntentClassifier>();
        services.AddSingleton<IResponseFormatter, ResponseFormatter>();
        services.AddSingleton<IContextProvider, ContextProvider>();
        services.AddSingleton<IAnalyticsContextProvider, AnalyticsContextProvider>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IInsightEngine, InsightEngine>();
        services.AddSingleton<IBusinessHealthService, BusinessHealthService>();
        services.AddSingleton<IRecommendationEngine, RecommendationEngine>();
        services.AddSingleton<INotificationInsightService, NotificationInsightService>();
        services.AddSingleton<ISummaryEngine, SummaryEngine>();
        services.AddSingleton<AiProviders.IAIProvider, AiProviders.MockAIProvider>();
        services.AddSingleton<AIOrchestrator>();
        services.AddSingleton<IAIService>(sp =>
            new AIServicePermissionGate(sp.GetRequiredService<AIOrchestrator>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IOrganizationQueryService, OrganizationQueryService>();
        services.AddSingleton<OrganizationCommandService>();
        services.AddSingleton<IOrganizationCommandService>(sp =>
            new OrganizationCommandServicePermissionGate(sp.GetRequiredService<OrganizationCommandService>(), sp.GetRequiredService<IPermissionGate>()));
        services.AddSingleton<IPermissionEngine, PermissionEngine>();

        // Phase 22A: Enterprise Context Migration - IEnterpriseContext
        // itself is registered by Shell (the composition root owns the
        // concrete, file-system-backed session), but PermissionGate is
        // pure Application-layer logic and belongs here, same as
        // PermissionEngine above.
        services.AddSingleton<IPermissionGate, PermissionGate>();

        // Phase 25: Enterprise Identity & Secure Client Platform. Pure
        // timing/control-flow logic with no I/O of its own - same
        // reasoning PermissionEngine/PermissionGate above already
        // establishes for infrastructure-free logic living in Application
        // rather than Infrastructure.
        services.AddSingleton<IRetryPolicy, RetryPolicy>();

        // Phase 26: Smart Context Help. HelpQueryService needs Infrastructure's
        // IHelpRepository (registered in AddInfrastructure()) but is itself
        // pure orchestration/mapping, same "interface+impl both in
        // Application, concrete repository comes from Infrastructure at
        // resolution time" shape every other module's QueryService uses.
        // HelpSearchService is pure matching/scoring logic with no I/O -
        // same reasoning RetryPolicy above already establishes.
        services.AddSingleton<IHelpQueryService, HelpQueryService>();
        services.AddSingleton<IHelpSearchService, HelpSearchService>();

        // Phase 27: Enterprise Notification Center. NotificationService needs
        // Infrastructure's INotificationRepository/ISilentModePreferenceStore
        // (registered in AddInfrastructure()) but is itself pure
        // orchestration/mapping/event-raising, the same shape HelpQueryService
        // above establishes. NotificationSearchService is pure matching/
        // scoring logic with no I/O - same reasoning HelpSearchService/
        // RetryPolicy already establish.
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<INotificationSearchService, NotificationSearchService>();

        // Phase 28: Enterprise Global Search & Command Palette.
        // GlobalSearchIndexService depends only on other Application-layer
        // query services already registered above (same layer, not a
        // violation) - see its own doc comment. SearchRankingService is
        // pure matching/scoring logic with no I/O - same reasoning
        // HelpSearchService/NotificationSearchService already establish.
        services.AddSingleton<IGlobalSearchIndexService, GlobalSearchIndexService>();
        services.AddSingleton<ISearchRankingService, SearchRankingService>();

        // Phase 29: Enterprise Workspace & Window Management. WorkspaceService
        // orchestrates saved layouts on top of Infrastructure's
        // IWorkspaceRepository - the same interface-to-concrete singleton
        // pattern every existing service in this app already uses.
        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        // Phase 32: Enterprise Automation, Workflow & Business Rules
        // Engine. WorkflowExecutionEngine depends on every registered
        // IWorkflowStepExecutor (resolved as IEnumerable<T>, one
        // registration per WorkflowStepType) - registration order here
        // doesn't matter, the engine keys them by StepType itself.
        // NoOpAiActionExecutor is pure logic with no I/O, so - unlike
        // IEmailNotificationService, which needs file-system access and is
        // registered in Infrastructure - it's registered directly here.
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddSingleton<IBusinessRuleService, BusinessRuleService>();
        services.AddSingleton<IScheduledJobService, ScheduledJobService>();
        services.AddSingleton<IApprovalService, ApprovalService>();
        services.AddSingleton<ITriggerEngine, TriggerEngine>();
        services.AddSingleton<IWorkflowExecutionEngine, WorkflowExecutionEngine>();
        services.AddSingleton<IAutomationDashboardQueryService, AutomationDashboardQueryService>();
        services.AddSingleton<IAiActionExecutor, NoOpAiActionExecutor>();

        services.AddSingleton<IWorkflowStepExecutor, StartStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, EndStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, DecisionStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, DelayStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, ApprovalStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, ConditionStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, NotificationStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, EmailStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, AiActionStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, DatabaseActionStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, ApiActionStepExecutor>();
        services.AddSingleton<IWorkflowStepExecutor, RunReportStepExecutor>();

        services.AddSingleton<ISupportMessageService, SupportMessageService>();
        services.AddSingleton<IDevelopmentApplicationService, DevelopmentApplicationService>();

        return services;
    }
}
