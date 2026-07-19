namespace Rojan.Desktop.Application.Reporting;

/// <summary>Outcome of <see cref="IReportExportService.ExportAsync"/> - <see cref="Success"/> false with an honest <see cref="Message"/> for the not-yet-implemented formats, never a silent no-op or an unhandled exception reaching the UI.</summary>
public sealed record ExportResultDto(bool Success, string Message, string? FilePath);
