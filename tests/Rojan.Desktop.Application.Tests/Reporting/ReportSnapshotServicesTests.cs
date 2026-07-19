using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportSnapshotServicesTests
{
    private static ReportResultDto MakeResult(string reportId = "revenue-report", int rowCount = 3) => new(
        reportId,
        "Revenue Report",
        DateTimeOffset.Now,
        [],
        Enumerable.Range(0, rowCount).Select(_ => new ReportRowDto(new Dictionary<string, string>())).ToList(),
        [],
        new Dictionary<string, string>());

    [Fact]
    public async Task RecordSnapshotAsync_CreatesUnsavedSnapshotWithCorrectRowCount()
    {
        var repository = new StubReportingRepository();
        var commandService = new ReportSnapshotCommandService(repository);

        var snapshot = await commandService.RecordSnapshotAsync(MakeResult(rowCount: 7));

        Assert.False(snapshot.IsSaved);
        Assert.Equal(7, snapshot.RowCount);
        Assert.Equal("revenue-report", snapshot.ReportDefinitionId);
    }

    [Fact]
    public async Task ToggleSavedAsync_FlipsIsSavedFlag()
    {
        var repository = new StubReportingRepository();
        var commandService = new ReportSnapshotCommandService(repository);
        var recorded = await commandService.RecordSnapshotAsync(MakeResult());

        var toggled = await commandService.ToggleSavedAsync(recorded.Id, true);

        Assert.True(toggled.IsSaved);
    }

    [Fact]
    public async Task DeleteSnapshotAsync_RemovesSnapshot()
    {
        var repository = new StubReportingRepository();
        var commandService = new ReportSnapshotCommandService(repository);
        var queryService = new ReportSnapshotQueryService(repository);
        var recorded = await commandService.RecordSnapshotAsync(MakeResult());

        await commandService.DeleteSnapshotAsync(recorded.Id);
        var recent = await queryService.GetRecentSnapshotsAsync();

        Assert.DoesNotContain(recent, s => s.Id == recorded.Id);
    }

    [Fact]
    public async Task GetSavedSnapshotsAsync_OnlyReturnsSavedOnes()
    {
        var repository = new StubReportingRepository();
        var commandService = new ReportSnapshotCommandService(repository);
        var queryService = new ReportSnapshotQueryService(repository);
        var unsaved = await commandService.RecordSnapshotAsync(MakeResult());
        var saved = await commandService.RecordSnapshotAsync(MakeResult());
        await commandService.ToggleSavedAsync(saved.Id, true);

        var savedSnapshots = await queryService.GetSavedSnapshotsAsync();

        Assert.Contains(savedSnapshots, s => s.Id == saved.Id);
        Assert.DoesNotContain(savedSnapshots, s => s.Id == unsaved.Id);
    }

    [Fact]
    public async Task GetRecentSnapshotsAsync_ReturnsNewestFirst()
    {
        var repository = new StubReportingRepository();
        var commandService = new ReportSnapshotCommandService(repository);
        var queryService = new ReportSnapshotQueryService(repository);
        await commandService.RecordSnapshotAsync(MakeResult());
        await commandService.RecordSnapshotAsync(MakeResult());

        var recent = await queryService.GetRecentSnapshotsAsync();

        Assert.Equal(recent.OrderByDescending(s => s.GeneratedAt).Select(s => s.Id), recent.Select(s => s.Id));
    }
}
