using System.Globalization;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;
using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// Default <see cref="IReportExecutionQueryService"/> - one aggregation
/// branch per <see cref="DomainReporting.ReportType"/>, each composing only
/// the sibling Application-layer query services it actually needs (never
/// every module's data is fetched for every report). Read-only throughout -
/// nothing here writes to any other module.
/// </summary>
public sealed class ReportExecutionQueryService : IReportExecutionQueryService
{
    private readonly DomainReporting.IReportingRepository _reportingRepository;
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly AppInventory.IProductQueryService _productQueryService;
    private readonly AppInventory.IInventoryQueryService _inventoryQueryService;
    private readonly AppAccounting.IInvoiceQueryService _invoiceQueryService;
    private readonly AppHr.IEmployeeQueryService _employeeQueryService;
    private readonly AppHr.IAttendanceQueryService _attendanceQueryService;
    private readonly AppHr.ICommissionQueryService _commissionQueryService;
    private readonly AppHr.IPayrollQueryService _payrollQueryService;

    public ReportExecutionQueryService(
        DomainReporting.IReportingRepository reportingRepository,
        AppCustomers.ICustomerQueryService customerQueryService,
        AppBookings.IBookingQueryService bookingQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        AppInventory.IProductQueryService productQueryService,
        AppInventory.IInventoryQueryService inventoryQueryService,
        AppAccounting.IInvoiceQueryService invoiceQueryService,
        AppHr.IEmployeeQueryService employeeQueryService,
        AppHr.IAttendanceQueryService attendanceQueryService,
        AppHr.ICommissionQueryService commissionQueryService,
        AppHr.IPayrollQueryService payrollQueryService)
    {
        _reportingRepository = reportingRepository;
        _customerQueryService = customerQueryService;
        _bookingQueryService = bookingQueryService;
        _serviceQueryService = serviceQueryService;
        _productQueryService = productQueryService;
        _inventoryQueryService = inventoryQueryService;
        _invoiceQueryService = invoiceQueryService;
        _employeeQueryService = employeeQueryService;
        _attendanceQueryService = attendanceQueryService;
        _commissionQueryService = commissionQueryService;
        _payrollQueryService = payrollQueryService;
    }

    public async Task<ReportResultDto> RunReportAsync(string reportDefinitionId, IReadOnlyList<ReportFilterDto> filters, CancellationToken cancellationToken = default)
    {
        var definition = await _reportingRepository.GetReportDefinitionByIdAsync(reportDefinitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Report definition '{reportDefinitionId}' was not found.");

        var filterSet = new ReportFilterSet(filters);
        var (rows, summary) = definition.ReportType switch
        {
            DomainReporting.ReportType.RevenueReport => await RunRevenueReportAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.SalesReport => await RunSalesReportAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.AppointmentsReport => await RunAppointmentsReportAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.CustomerGrowth => await RunCustomerGrowthAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.CustomerRetention => await RunCustomerRetentionAsync(cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.ServicePopularity => await RunServicePopularityAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.SpecialistPerformance => await RunSpecialistPerformanceAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.InventoryValuation => await RunInventoryValuationAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.LowStock => await RunLowStockAsync(cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.PayrollSummary => await RunPayrollSummaryAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.CommissionSummary => await RunCommissionSummaryAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.AttendanceSummary => await RunAttendanceSummaryAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.DailyDashboard => await RunDashboardAsync(AnalyticsPeriod.Daily, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.WeeklyDashboard => await RunDashboardAsync(AnalyticsPeriod.Weekly, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.MonthlyDashboard => await RunDashboardAsync(AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(reportDefinitionId), definition.ReportType, "Unknown report type."),
        };

        return new ReportResultDto(
            definition.Id,
            definition.Name,
            DateTimeOffset.Now,
            definition.Columns.Select(ReportingMapper.MapColumn).ToList(),
            rows,
            filters,
            summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunRevenueReportAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = invoices.Where(invoice => filterSet.IsWithinDateRange(invoice.IssuedAt)).ToList();

        var rows = filtered
            .GroupBy(invoice => DateOnly.FromDateTime(invoice.IssuedAt.Date))
            .OrderBy(group => group.Key)
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["date"] = group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["invoiceCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["totalRevenue"] = FormatCurrency(group.Sum(invoice => invoice.Total)),
            }))
            .ToList();

        var summary = new Dictionary<string, string>
        {
            ["درآمد کل"] = FormatCurrency(filtered.Sum(invoice => invoice.Total)),
            ["تعداد فاکتور"] = filtered.Count.ToString(CultureInfo.InvariantCulture),
        };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunSalesReportAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = invoices
            .Where(invoice => filterSet.IsWithinDateRange(invoice.IssuedAt))
            .Where(invoice => filterSet.Matches(FilterType.Customer, invoice.CustomerId))
            .Where(invoice => filterSet.Matches(FilterType.Status, invoice.Status.ToString()))
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ToList();

        var rows = filtered.Select(invoice => new ReportRowDto(new Dictionary<string, string>
        {
            ["invoiceId"] = invoice.Id,
            ["customerName"] = invoice.CustomerName,
            ["issuedAt"] = invoice.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["status"] = invoice.Status.ToString(),
            ["total"] = FormatCurrency(invoice.Total),
        })).ToList();

        var summary = new Dictionary<string, string>
        {
            ["جمع کل"] = FormatCurrency(filtered.Sum(invoice => invoice.Total)),
            ["تعداد فاکتور"] = filtered.Count.ToString(CultureInfo.InvariantCulture),
        };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunAppointmentsReportAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = bookings
            .Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt))
            .Where(booking => filterSet.Matches(FilterType.Specialist, booking.SpecialistId))
            .Where(booking => filterSet.Matches(FilterType.Service, booking.ServiceId))
            .Where(booking => filterSet.Matches(FilterType.Status, booking.Status.ToString()))
            .Where(booking => filterSet.Matches(FilterType.Customer, booking.CustomerId))
            .OrderByDescending(booking => booking.ScheduledAt)
            .ToList();

        var rows = filtered.Select(booking => new ReportRowDto(new Dictionary<string, string>
        {
            ["date"] = booking.ScheduledAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            ["customerName"] = booking.CustomerName,
            ["serviceName"] = booking.ServiceName,
            ["specialistName"] = booking.SpecialistName,
            ["status"] = booking.Status.ToString(),
            ["price"] = FormatCurrency(MoneyParser.Parse(booking.Price)),
        })).ToList();

        var summary = new Dictionary<string, string>
        {
            ["نوبت‌ها"] = filtered.Count.ToString(CultureInfo.InvariantCulture),
            ["ارزش کل"] = FormatCurrency(filtered.Sum(booking => MoneyParser.Parse(booking.Price))),
        };
        return (rows, summary);
    }

    /// <summary>
    /// New-customers-per-month, using <c>CustomerDto.LastContactedAt</c> as
    /// a proxy for "created" - Customers has no created-date field, so this
    /// is documented as an approximation rather than a true acquisition
    /// date, same honesty convention the migration report used elsewhere
    /// in this app for data gaps.
    /// </summary>
    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunCustomerGrowthAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(false);
        var filtered = customers.Where(customer => filterSet.IsWithinDateRange(customer.LastContactedAt)).ToList();

        var byMonth = filtered
            .GroupBy(customer => new DateOnly(customer.LastContactedAt.Year, customer.LastContactedAt.Month, 1))
            .OrderBy(group => group.Key)
            .ToList();

        var running = 0;
        var rows = new List<ReportRowDto>();
        foreach (var group in byMonth)
        {
            running += group.Count();
            rows.Add(new ReportRowDto(new Dictionary<string, string>
            {
                ["period"] = group.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                ["newCustomers"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["totalCustomers"] = running.ToString(CultureInfo.InvariantCulture),
            }));
        }

        var summary = new Dictionary<string, string> { ["مشتریان جدید"] = filtered.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunCustomerRetentionAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(false);
        var total = customers.Count;

        var rows = customers
            .GroupBy(customer => customer.Status)
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["status"] = group.Key.ToString(),
                ["count"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["percentage"] = FormatPercentage(total == 0 ? 0m : Math.Round((decimal)group.Count() / total * 100m, 1)),
            }))
            .ToList();

        var retained = customers.Count(customer =>
            customer.Status is AppCustomers.CustomerStatus.Active or AppCustomers.CustomerStatus.Vip or AppCustomers.CustomerStatus.Prospect or AppCustomers.CustomerStatus.Lead);
        var summary = new Dictionary<string, string>
        {
            ["نرخ بازگشت"] = FormatPercentage(total == 0 ? 0m : Math.Round((decimal)retained / total * 100m, 1)),
        };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunServicePopularityAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var services = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(false);
        var servicesById = services.ToDictionary(service => service.Id);

        var filtered = bookings
            .Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt))
            .Where(booking => filterSet.Matches(FilterType.Service, booking.ServiceId))
            .Where(booking => !servicesById.TryGetValue(booking.ServiceId, out var service) || filterSet.Matches(FilterType.Category, service.Category.ToString()))
            .ToList();

        var rows = filtered
            .GroupBy(booking => booking.ServiceName)
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["serviceName"] = group.Key,
                ["bookingCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["revenue"] = FormatCurrency(group.Sum(booking => MoneyParser.Parse(booking.Price))),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["نوبت‌ها"] = filtered.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunSpecialistPerformanceAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = bookings
            .Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt))
            .Where(booking => filterSet.Matches(FilterType.Specialist, booking.SpecialistId))
            .ToList();

        var employees = await _employeeQueryService.GetEmployeesAsync(cancellationToken).ConfigureAwait(false);
        var commissions = await _commissionQueryService.GetAllCommissionTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var periodCommissions = commissions.Where(transaction => filterSet.IsWithinDateRange(transaction.EarnedAt)).ToList();

        var rows = filtered
            .GroupBy(booking => new { booking.SpecialistId, booking.SpecialistName })
            .OrderByDescending(group => group.Sum(booking => MoneyParser.Parse(booking.Price)))
            .Select(group =>
            {
                var employee = employees.FirstOrDefault(e => e.SpecialistId == group.Key.SpecialistId);
                var commissionEarned = employee is null ? 0m : periodCommissions.Where(t => t.EmployeeId == employee.Id).Sum(t => t.CommissionAmount);
                return new ReportRowDto(new Dictionary<string, string>
                {
                    ["specialistName"] = group.Key.SpecialistName,
                    ["bookingCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                    ["revenue"] = FormatCurrency(group.Sum(booking => MoneyParser.Parse(booking.Price))),
                    ["commissionEarned"] = FormatCurrency(commissionEarned),
                });
            })
            .ToList();

        var summary = new Dictionary<string, string> { ["متخصصان"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunInventoryValuationAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var products = await _productQueryService.GetProductsAsync(cancellationToken).ConfigureAwait(false);
        var items = await _inventoryQueryService.GetInventoryItemsAsync(cancellationToken).ConfigureAwait(false);
        var productsById = products.ToDictionary(product => product.Id);

        var rows = new List<ReportRowDto>();
        var totalValue = 0m;
        foreach (var item in items)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product) || !filterSet.Matches(FilterType.Category, product.CategoryId))
            {
                continue;
            }

            var unitPrice = MoneyParser.Parse(product.UnitPrice);
            var value = item.QuantityOnHand * unitPrice;
            totalValue += value;
            rows.Add(new ReportRowDto(new Dictionary<string, string>
            {
                ["productName"] = item.ProductName,
                ["quantityOnHand"] = item.QuantityOnHand.ToString(CultureInfo.InvariantCulture),
                ["unitPrice"] = FormatCurrency(unitPrice),
                ["totalValue"] = FormatCurrency(value),
            }));
        }

        var summary = new Dictionary<string, string> { ["ارزش کل"] = FormatCurrency(totalValue) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunLowStockAsync(CancellationToken cancellationToken)
    {
        var items = await _inventoryQueryService.GetLowStockItemsAsync(cancellationToken).ConfigureAwait(false);
        var rows = items.Select(item => new ReportRowDto(new Dictionary<string, string>
        {
            ["productName"] = item.ProductName,
            ["quantityOnHand"] = item.QuantityOnHand.ToString(CultureInfo.InvariantCulture),
            ["reorderThreshold"] = item.ReorderThreshold.ToString(CultureInfo.InvariantCulture),
            ["shortfall"] = Math.Max(0, item.ReorderThreshold - item.QuantityOnHand).ToString(CultureInfo.InvariantCulture),
        })).ToList();

        var summary = new Dictionary<string, string> { ["اقلام رو به اتمام"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunPayrollSummaryAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var summaries = await _payrollQueryService.GetPayrollSummariesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = summaries
            .Where(summary => filterSet.Matches(FilterType.Employee, summary.EmployeeId))
            .Where(summary => filterSet.IsWithinDateRange(new DateTimeOffset(summary.Year, summary.Month, 1, 0, 0, 0, TimeSpan.Zero)))
            .OrderByDescending(summary => summary.Year).ThenByDescending(summary => summary.Month)
            .ToList();

        var rows = filtered.Select(summary => new ReportRowDto(new Dictionary<string, string>
        {
            ["employeeName"] = summary.EmployeeName,
            ["period"] = $"{summary.Month:D2}/{summary.Year:D4}",
            ["baseSalary"] = FormatCurrency(summary.BaseSalary),
            ["commissionTotal"] = FormatCurrency(summary.CommissionTotal),
            ["bonus"] = FormatCurrency(summary.Bonus),
            ["deduction"] = FormatCurrency(summary.Deduction),
            ["netSalary"] = FormatCurrency(summary.NetSalary),
        })).ToList();

        var summaryTotals = new Dictionary<string, string> { ["جمع خالص حقوق"] = FormatCurrency(filtered.Sum(summary => summary.NetSalary)) };
        return (rows, summaryTotals);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunCommissionSummaryAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var transactions = await _commissionQueryService.GetAllCommissionTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = transactions
            .Where(transaction => filterSet.Matches(FilterType.Employee, transaction.EmployeeId))
            .Where(transaction => filterSet.IsWithinDateRange(transaction.EarnedAt))
            .ToList();

        var rows = filtered
            .GroupBy(transaction => transaction.EmployeeName)
            .OrderByDescending(group => group.Sum(t => t.CommissionAmount))
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["employeeName"] = group.Key,
                ["transactionCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["totalCommission"] = FormatCurrency(group.Sum(t => t.CommissionAmount)),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["جمع کمیسیون"] = FormatCurrency(filtered.Sum(t => t.CommissionAmount)) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunAttendanceSummaryAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var employees = await _employeeQueryService.GetEmployeesAsync(cancellationToken).ConfigureAwait(false);
        var filteredEmployees = employees.Where(employee => filterSet.Matches(FilterType.Employee, employee.Id)).ToList();

        // Fetched concurrently, not one employee at a time - see
        // AnalyticsAggregator.AggregateAsync's identical fix for why this
        // matters against FakeHrRepository's per-call artificial delays.
        var attendanceByEmployee = await Task.WhenAll(
            filteredEmployees.Select(employee => _attendanceQueryService.GetAttendanceForEmployeeAsync(employee.Id, cancellationToken)))
            .ConfigureAwait(false);

        var rows = new List<ReportRowDto>();
        for (var i = 0; i < filteredEmployees.Count; i++)
        {
            var employee = filteredEmployees[i];
            var periodRecords = attendanceByEmployee[i].Where(record => filterSet.IsWithinDateRange(record.Date.ToDateTime(TimeOnly.MinValue))).ToList();
            if (periodRecords.Count == 0)
            {
                continue;
            }

            var present = periodRecords.Count(record => record.Status == AppHr.AttendanceStatus.Present);
            var late = periodRecords.Count(record => record.Status == AppHr.AttendanceStatus.Late);
            var absent = periodRecords.Count(record => record.Status == AppHr.AttendanceStatus.Absent);
            var rate = Math.Round((decimal)present / periodRecords.Count * 100m, 1);

            rows.Add(new ReportRowDto(new Dictionary<string, string>
            {
                ["employeeName"] = employee.FullName,
                ["presentCount"] = present.ToString(CultureInfo.InvariantCulture),
                ["lateCount"] = late.ToString(CultureInfo.InvariantCulture),
                ["absentCount"] = absent.ToString(CultureInfo.InvariantCulture),
                ["attendanceRate"] = FormatPercentage(rate),
            }));
        }

        var summary = new Dictionary<string, string> { ["کارمندان"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunDashboardAsync(AnalyticsPeriod period, CancellationToken cancellationToken)
    {
        var aggregator = new AnalyticsAggregator(
            _customerQueryService, _bookingQueryService, _productQueryService, _inventoryQueryService,
            _invoiceQueryService, _employeeQueryService, _attendanceQueryService, _payrollQueryService);
        var (start, end, label) = AnalyticsPeriods.Resolve(period);
        var summary = await aggregator.AggregateAsync(start, end, label, cancellationToken).ConfigureAwait(false);

        var rows = new List<ReportRowDto>
        {
            MetricRow("درآمد کل", FormatCurrency(summary.TotalRevenue)),
            MetricRow("نوبت‌ها", summary.TotalAppointments.ToString(CultureInfo.InvariantCulture)),
            MetricRow("تعداد مشتریان", summary.TotalCustomers.ToString(CultureInfo.InvariantCulture)),
            MetricRow("مشتریان جدید", summary.NewCustomers.ToString(CultureInfo.InvariantCulture)),
            MetricRow("نرخ بازگشت", FormatPercentage(summary.RetentionRatePercent)),
            MetricRow("محبوب‌ترین خدمت", summary.TopServiceName),
            MetricRow("برترین متخصص", summary.TopSpecialistName),
            MetricRow("ارزش موجودی انبار", FormatCurrency(summary.InventoryValue)),
            MetricRow("اقلام رو به اتمام", summary.LowStockCount.ToString(CultureInfo.InvariantCulture)),
            MetricRow("جمع حقوق (این ماه)", FormatCurrency(summary.PayrollTotal)),
            MetricRow("نرخ حضور", FormatPercentage(summary.AttendanceRatePercent)),
        };

        return (rows, new Dictionary<string, string> { ["دوره"] = label });
    }

    private static ReportRowDto MetricRow(string metric, string value) =>
        new(new Dictionary<string, string> { ["metric"] = metric, ["value"] = value });

    // "C2" under CultureInfo.InvariantCulture renders the generic currency
    // sign ("¤"), not a real symbol - InvariantCulture has no real currency
    // assigned. An explicit "تومان" suffix avoids that, consistent with how
    // every other read-only module's display-string money (Bookings.Price,
    // Services.Price, ...) is formatted in this app.
    private static string FormatCurrency(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture) + " تومان";

    private static string FormatPercentage(decimal value) => value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
}
