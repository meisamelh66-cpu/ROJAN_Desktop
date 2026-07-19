using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportCatalogQueryServiceTests
{
    [Fact]
    public async Task GetReportDefinitionsAsync_ReturnsMappedDtos()
    {
        var sut = new ReportCatalogQueryService(new StubReportingRepository());

        var definitions = await sut.GetReportDefinitionsAsync();

        Assert.NotEmpty(definitions);
        Assert.Contains(definitions, d => d.Id == "revenue-report" && d.ReportType == ReportType.RevenueReport);
    }

    [Fact]
    public async Task GetReportDefinitionByIdAsync_WithKnownId_ReturnsMappedDto()
    {
        var sut = new ReportCatalogQueryService(new StubReportingRepository());

        var definition = await sut.GetReportDefinitionByIdAsync("low-stock");

        Assert.NotNull(definition);
        Assert.Equal(ReportType.LowStock, definition.ReportType);
    }

    [Fact]
    public async Task GetReportDefinitionByIdAsync_WithUnknownId_ReturnsNull()
    {
        var sut = new ReportCatalogQueryService(new StubReportingRepository());

        var definition = await sut.GetReportDefinitionByIdAsync("does-not-exist");

        Assert.Null(definition);
    }
}
