using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportExportServiceTests
{
    private static ReportResultDto MakeResult() => new(
        "revenue-report",
        "Revenue Report",
        new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero),
        [
            new ReportColumnDto("date", "Date", ReportColumnDataType.Date),
            new ReportColumnDto("totalRevenue", "Total Revenue", ReportColumnDataType.Currency),
        ],
        [
            new ReportRowDto(new Dictionary<string, string> { ["date"] = "2026-07-19", ["totalRevenue"] = "$294.00" }),
            new ReportRowDto(new Dictionary<string, string> { ["date"] = "2026-07-18", ["totalRevenue"] = "$286.00" }),
        ],
        [],
        new Dictionary<string, string>());

    [Fact]
    public async Task ExportAsync_Csv_WritesRealFileWithHeaderAndRows()
    {
        var sut = new ReportExportService();

        var result = await sut.ExportAsync(MakeResult(), ExportFormat.Csv);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath));

        var content = await File.ReadAllTextAsync(result.FilePath);
        Assert.Contains("Date,Total Revenue", content, StringComparison.Ordinal);
        Assert.Contains("2026-07-19,$294.00", content, StringComparison.Ordinal);

        File.Delete(result.FilePath);
    }

    [Theory]
    [InlineData(ExportFormat.Pdf)]
    [InlineData(ExportFormat.Excel)]
    [InlineData(ExportFormat.Print)]
    public async Task ExportAsync_UnimplementedFormats_ReturnHonestFailureNotException(ExportFormat format)
    {
        var sut = new ReportExportService();

        var result = await sut.ExportAsync(MakeResult(), format);

        Assert.False(result.Success);
        Assert.Null(result.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}
