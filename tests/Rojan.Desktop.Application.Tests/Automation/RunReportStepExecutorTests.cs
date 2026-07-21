using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Application.Reporting;
using Xunit;

namespace Rojan.Desktop.Application.Tests.Automation;

public sealed class RunReportStepExecutorTests
{
    private sealed class StubReportExecutionQueryService : IReportExecutionQueryService
    {
        public ReportResultDto? Result { get; set; }
        public bool ThrowNotFound { get; set; }

        public Task<ReportResultDto> RunReportAsync(string reportDefinitionId, IReadOnlyList<ReportFilterDto> filters, CancellationToken cancellationToken = default)
        {
            if (ThrowNotFound)
            {
                throw new InvalidOperationException($"Report definition '{reportDefinitionId}' was not found.");
            }

            return Task.FromResult(Result!);
        }
    }

    private sealed class StubReportExportService : IReportExportService
    {
        public ExportResultDto Response { get; set; } = new(true, "OK", "C:\\reports\\out.csv");
        public int CallCount { get; private set; }

        public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Response);
        }
    }

    private static ReportResultDto SampleResult() => new(
        "cash-flow",
        "جریان نقدی",
        DateTimeOffset.UtcNow,
        [],
        [],
        [],
        new Dictionary<string, string>());

    private static WorkflowStepDto StepWithConfig(IReadOnlyDictionary<string, string> config) =>
        new("run-report-1", WorkflowStepType.RunReport, "Run Report", config, null, null);

    [Fact]
    public async Task ExecuteAsync_MissingReportDefinitionId_ReturnsFailure()
    {
        var executor = new RunReportStepExecutor(new StubReportExecutionQueryService(), new StubReportExportService(), new StubEmailNotificationService());
        var step = StepWithConfig(new Dictionary<string, string>());

        var result = await executor.ExecuteAsync(step, new AutomationExecutionContext("exec-1", new Dictionary<string, string>(), "org-1", "branch-1", "user-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("reportDefinitionId", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownReportDefinitionId_ReturnsFailure()
    {
        var queryService = new StubReportExecutionQueryService { ThrowNotFound = true };
        var executor = new RunReportStepExecutor(queryService, new StubReportExportService(), new StubEmailNotificationService());
        var step = StepWithConfig(new Dictionary<string, string> { ["reportDefinitionId"] = "missing-report" });

        var result = await executor.ExecuteAsync(step, new AutomationExecutionContext("exec-1", new Dictionary<string, string>(), "org-1", "branch-1", "user-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("missing-report", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ValidReport_ExportsCsvAndSucceeds()
    {
        var queryService = new StubReportExecutionQueryService { Result = SampleResult() };
        var exportService = new StubReportExportService();
        var executor = new RunReportStepExecutor(queryService, exportService, new StubEmailNotificationService());
        var step = StepWithConfig(new Dictionary<string, string> { ["reportDefinitionId"] = "cash-flow" });

        var result = await executor.ExecuteAsync(step, new AutomationExecutionContext("exec-1", new Dictionary<string, string>(), "org-1", "branch-1", "user-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, exportService.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithRecipientEmail_SendsEmailWithReportName()
    {
        var queryService = new StubReportExecutionQueryService { Result = SampleResult() };
        var emailService = new StubEmailNotificationService();
        var executor = new RunReportStepExecutor(queryService, new StubReportExportService(), emailService);
        var step = StepWithConfig(new Dictionary<string, string>
        {
            ["reportDefinitionId"] = "cash-flow",
            ["recipientEmail"] = "owner@rojanai.ir",
        });

        var result = await executor.ExecuteAsync(step, new AutomationExecutionContext("exec-1", new Dictionary<string, string>(), "org-1", "branch-1", "user-1"));

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(emailService.SentMessages);
        Assert.Equal("owner@rojanai.ir", sent.ToAddress);
        Assert.Contains("جریان نقدی", sent.Subject);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRecipientEmail_DoesNotSendEmail()
    {
        var queryService = new StubReportExecutionQueryService { Result = SampleResult() };
        var emailService = new StubEmailNotificationService();
        var executor = new RunReportStepExecutor(queryService, new StubReportExportService(), emailService);
        var step = StepWithConfig(new Dictionary<string, string> { ["reportDefinitionId"] = "cash-flow" });

        await executor.ExecuteAsync(step, new AutomationExecutionContext("exec-1", new Dictionary<string, string>(), "org-1", "branch-1", "user-1"));

        Assert.Empty(emailService.SentMessages);
    }
}
