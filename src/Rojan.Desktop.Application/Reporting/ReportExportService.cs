using System.Text;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>Default <see cref="IReportExportService"/>. See that interface's doc comment for the "Csv is real, the rest are honest stubs" scope decision.</summary>
public sealed class ReportExportService : IReportExportService
{
    public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken cancellationToken = default) => format switch
    {
        ExportFormat.Csv => Task.FromResult(ExportCsv(result)),
        ExportFormat.Pdf => Task.FromResult(NotYetImplemented("PDF")),
        ExportFormat.Excel => Task.FromResult(NotYetImplemented("Excel")),
        ExportFormat.Print => Task.FromResult(NotYetImplemented("Print")),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown export format."),
    };

    private static ExportResultDto ExportCsv(ReportResultDto result)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns.Select(column => EscapeCsv(column.Header))));
        foreach (var row in result.Rows)
        {
            var cells = result.Columns.Select(column => EscapeCsv(row.Values.GetValueOrDefault(column.Key, string.Empty)));
            builder.AppendLine(string.Join(",", cells));
        }

        var directory = Path.Combine(Path.GetTempPath(), "RojanDesktopExports");
        Directory.CreateDirectory(directory);
        var fileName = $"{result.ReportName.Replace(' ', '_')}_{result.GeneratedAt:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);

        return new ExportResultDto(true, $"Exported {result.Rows.Count} rows to CSV.", filePath);
    }

    private static ExportResultDto NotYetImplemented(string formatName) =>
        new(false, $"{formatName} export is not yet implemented - Phase 20 ships the export architecture only.", null);

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
