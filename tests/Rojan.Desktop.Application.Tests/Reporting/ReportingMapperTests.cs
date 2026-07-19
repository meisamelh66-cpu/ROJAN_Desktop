using Rojan.Desktop.Application.Reporting;
using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportingMapperTests
{
    [Theory]
    [InlineData(DomainReporting.ReportType.RevenueReport, ReportType.RevenueReport)]
    [InlineData(DomainReporting.ReportType.MonthlyDashboard, ReportType.MonthlyDashboard)]
    public void MapReportType_MapsToEquivalentApplicationEnum(DomainReporting.ReportType domainType, ReportType expected)
    {
        Assert.Equal(expected, ReportingMapper.MapReportType(domainType));
    }

    [Theory]
    [InlineData(DomainReporting.FilterType.DateRange, FilterType.DateRange)]
    [InlineData(DomainReporting.FilterType.Specialist, FilterType.Specialist)]
    public void MapFilterType_DomainToApplication_RoundTrips(DomainReporting.FilterType domainType, FilterType expected)
    {
        var mapped = ReportingMapper.MapFilterType(domainType);
        Assert.Equal(expected, mapped);
        Assert.Equal(domainType, ReportingMapper.MapFilterType(mapped));
    }

    [Fact]
    public void MapDefinition_MapsAllFieldsAndNestedColumns()
    {
        var domainDefinition = new DomainReporting.ReportDefinition(
            "revenue-report",
            "Revenue Report",
            "desc",
            DomainReporting.ReportType.RevenueReport,
            DomainReporting.ReportCategory.Financial,
            true,
            [new DomainReporting.ReportColumn("date", "Date", DomainReporting.ReportColumnDataType.Date)],
            [DomainReporting.FilterType.DateRange]);

        var dto = ReportingMapper.MapDefinition(domainDefinition);

        Assert.Equal("revenue-report", dto.Id);
        Assert.Equal(ReportType.RevenueReport, dto.ReportType);
        Assert.Equal(ReportCategory.Financial, dto.Category);
        Assert.Single(dto.Columns);
        Assert.Equal("date", dto.Columns[0].Key);
        Assert.Equal(ReportColumnDataType.Date, dto.Columns[0].DataType);
        Assert.Single(dto.SupportedFilters);
        Assert.Equal(FilterType.DateRange, dto.SupportedFilters[0]);
    }

    [Fact]
    public void MapSnapshot_MapsAllFields()
    {
        var domainSnapshot = new DomainReporting.ReportSnapshot(
            "snapshot-1", "revenue-report", "Revenue Report", DateTimeOffset.Now,
            [new DomainReporting.ReportFilter(DomainReporting.FilterType.Status, "Paid", "Status: Paid")], 5, true);

        var dto = ReportingMapper.MapSnapshot(domainSnapshot);

        Assert.Equal("snapshot-1", dto.Id);
        Assert.Equal(5, dto.RowCount);
        Assert.True(dto.IsSaved);
        Assert.Single(dto.AppliedFilters);
        Assert.Equal("Paid", dto.AppliedFilters[0].Value);
    }

    [Fact]
    public void MapTrend_MapsAllThreeDirections()
    {
        Assert.Equal(TrendDirection.Up, ReportingMapper.MapTrend(DomainReporting.TrendDirection.Up));
        Assert.Equal(TrendDirection.Down, ReportingMapper.MapTrend(DomainReporting.TrendDirection.Down));
        Assert.Equal(TrendDirection.Flat, ReportingMapper.MapTrend(DomainReporting.TrendDirection.Flat));
    }
}
