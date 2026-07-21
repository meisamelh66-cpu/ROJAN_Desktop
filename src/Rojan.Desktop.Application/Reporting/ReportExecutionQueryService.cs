using System.Globalization;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppAI = Rojan.Desktop.Application.AI;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppOrganizations = Rojan.Desktop.Application.Organizations;
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
    private readonly AppHr.IShiftQueryService _shiftQueryService;
    private readonly AppOrganizations.IOrganizationQueryService _organizationQueryService;
    private readonly AppAI.ITokenUsageTracker _tokenUsageTracker;

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
        AppHr.IPayrollQueryService payrollQueryService,
        AppHr.IShiftQueryService shiftQueryService,
        AppOrganizations.IOrganizationQueryService organizationQueryService,
        AppAI.ITokenUsageTracker tokenUsageTracker)
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
        _shiftQueryService = shiftQueryService;
        _organizationQueryService = organizationQueryService;
        _tokenUsageTracker = tokenUsageTracker;
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
            DomainReporting.ReportType.CashFlow => await RunCashFlowAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.OutstandingPayments => await RunOutstandingPaymentsAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.TaxSummary => await RunTaxSummaryAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.VipCustomers => await RunVipCustomersAsync(cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.InactiveCustomers => await RunInactiveCustomersAsync(cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.CustomerLifetimeValue => await RunCustomerLifetimeValueAsync(cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.AppointmentStatusBreakdown => await RunAppointmentStatusBreakdownAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.PeakHours => await RunPeakHoursAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.InventoryMovements => await RunInventoryMovementsAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.SupplierPurchases => await RunSupplierPurchasesAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.EmployeeWorkingHours => await RunEmployeeWorkingHoursAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.BranchPerformance => await RunBranchPerformanceAsync(filterSet, cancellationToken).ConfigureAwait(false),
            DomainReporting.ReportType.AiUsageSummary => await RunAiUsageSummaryAsync(cancellationToken).ConfigureAwait(false),
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

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunCashFlowAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = invoices
            .Where(invoice => filterSet.IsWithinDateRange(invoice.IssuedAt))
            .Where(invoice => invoice.Status is AppAccounting.InvoiceStatus.Paid or AppAccounting.InvoiceStatus.PartiallyPaid)
            .ToList();

        var rows = filtered
            .GroupBy(invoice => DateOnly.FromDateTime(invoice.IssuedAt.Date))
            .OrderBy(group => group.Key)
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["date"] = group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["cashIn"] = FormatCurrency(group.Sum(invoice => invoice.Total)),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["جریان نقدی خالص"] = FormatCurrency(filtered.Sum(invoice => invoice.Total)) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunOutstandingPaymentsAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = invoices
            .Where(invoice => filterSet.IsWithinDateRange(invoice.IssuedAt))
            .Where(invoice => invoice.Status is AppAccounting.InvoiceStatus.Issued or AppAccounting.InvoiceStatus.PartiallyPaid)
            .Where(invoice => filterSet.Matches(FilterType.Customer, invoice.CustomerId))
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ToList();

        var rows = filtered.Select(invoice => new ReportRowDto(new Dictionary<string, string>
        {
            ["invoiceId"] = invoice.Id,
            ["customerName"] = invoice.CustomerName,
            ["issuedAt"] = invoice.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["status"] = invoice.Status.ToString(),
            ["outstanding"] = FormatCurrency(invoice.Total),
        })).ToList();

        var summary = new Dictionary<string, string> { ["جمع مطالبات"] = FormatCurrency(filtered.Sum(invoice => invoice.Total)) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunTaxSummaryAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var filtered = invoices.Where(invoice => filterSet.IsWithinDateRange(invoice.IssuedAt)).ToList();

        var rows = filtered
            .GroupBy(invoice => new DateOnly(invoice.IssuedAt.Year, invoice.IssuedAt.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["period"] = group.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                ["taxableAmount"] = FormatCurrency(group.Sum(invoice => invoice.Subtotal)),
                ["taxCollected"] = FormatCurrency(group.Sum(invoice => invoice.TaxAmount)),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["جمع مالیات"] = FormatCurrency(filtered.Sum(invoice => invoice.TaxAmount)) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunVipCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(false);
        var filtered = customers.Where(customer => customer.Status == AppCustomers.CustomerStatus.Vip)
            .OrderByDescending(customer => MoneyParser.Parse(customer.LifetimeValue))
            .ToList();

        var rows = filtered.Select(customer => new ReportRowDto(new Dictionary<string, string>
        {
            ["name"] = customer.FullName,
            ["company"] = customer.Company,
            ["lifetimeValue"] = customer.LifetimeValue,
            ["lastContacted"] = customer.LastContactedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        })).ToList();

        var summary = new Dictionary<string, string> { ["مشتریان ویژه"] = filtered.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunInactiveCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(false);
        var filtered = customers.Where(customer => customer.Status == AppCustomers.CustomerStatus.Inactive)
            .OrderBy(customer => customer.LastContactedAt)
            .ToList();

        var rows = filtered.Select(customer => new ReportRowDto(new Dictionary<string, string>
        {
            ["name"] = customer.FullName,
            ["lastContacted"] = customer.LastContactedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["phone"] = customer.Phone,
        })).ToList();

        var summary = new Dictionary<string, string> { ["مشتریان غیرفعال"] = filtered.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunCustomerLifetimeValueAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(false);
        var ordered = customers.OrderByDescending(customer => MoneyParser.Parse(customer.LifetimeValue)).ToList();

        var rows = ordered.Select(customer => new ReportRowDto(new Dictionary<string, string>
        {
            ["name"] = customer.FullName,
            ["status"] = customer.Status.ToString(),
            ["lifetimeValue"] = customer.LifetimeValue,
        })).ToList();

        var summary = new Dictionary<string, string> { ["جمع ارزش مشتریان"] = FormatCurrency(ordered.Sum(customer => MoneyParser.Parse(customer.LifetimeValue))) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunAppointmentStatusBreakdownAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = bookings.Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt)).ToList();
        var total = filtered.Count;

        var rows = filtered
            .GroupBy(booking => booking.Status)
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["status"] = group.Key.ToString(),
                ["count"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["percentage"] = FormatPercentage(total == 0 ? 0m : Math.Round((decimal)group.Count() / total * 100m, 1)),
            }))
            .ToList();

        var cancelled = filtered.Count(booking => booking.Status == AppBookings.BookingStatus.Cancelled);
        var summary = new Dictionary<string, string>
        {
            ["نرخ لغو"] = FormatPercentage(total == 0 ? 0m : Math.Round((decimal)cancelled / total * 100m, 1)),
        };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunPeakHoursAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = bookings.Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt)).ToList();

        var rows = filtered
            .GroupBy(booking => booking.ScheduledAt.Hour)
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["hour"] = $"{group.Key:D2}:00",
                ["bookingCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
            }))
            .ToList();

        var busiestDay = filtered
            .GroupBy(booking => booking.ScheduledAt.DayOfWeek)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key.ToString())
            .FirstOrDefault() ?? string.Empty;

        var summary = new Dictionary<string, string> { ["شلوغ‌ترین روز"] = busiestDay };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunInventoryMovementsAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var transactions = await _inventoryQueryService.GetAllTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = transactions.Where(transaction => filterSet.IsWithinDateRange(transaction.OccurredAt)).ToList();

        var rows = filtered.Select(transaction => new ReportRowDto(new Dictionary<string, string>
        {
            ["date"] = transaction.OccurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["productName"] = transaction.ProductName,
            ["type"] = transaction.Type.ToString(),
            ["quantity"] = transaction.Quantity.ToString(CultureInfo.InvariantCulture),
        })).ToList();

        var summary = new Dictionary<string, string> { ["تعداد تراکنش"] = filtered.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunSupplierPurchasesAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var transactions = await _inventoryQueryService.GetAllTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var products = await _productQueryService.GetProductsAsync(cancellationToken).ConfigureAwait(false);
        var productsById = products.ToDictionary(product => product.Id);

        var received = transactions
            .Where(transaction => transaction.Type == AppInventory.StockTransactionType.Received)
            .Where(transaction => filterSet.IsWithinDateRange(transaction.OccurredAt))
            .Where(transaction => !productsById.TryGetValue(transaction.ProductId, out var product) || filterSet.Matches(FilterType.Supplier, product.SupplierId))
            .ToList();

        var rows = received
            .GroupBy(transaction => productsById.TryGetValue(transaction.ProductId, out var product) ? product.SupplierName : string.Empty)
            .OrderByDescending(group => group.Sum(t => t.Quantity))
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["supplierName"] = group.Key,
                ["transactionCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["totalQuantity"] = group.Sum(t => t.Quantity).ToString(CultureInfo.InvariantCulture),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["تأمین‌کنندگان"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunEmployeeWorkingHoursAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var assignments = await _shiftQueryService.GetAllShiftAssignmentsAsync(cancellationToken).ConfigureAwait(false);
        var shifts = await _shiftQueryService.GetShiftsAsync(cancellationToken).ConfigureAwait(false);
        var shiftsById = shifts.ToDictionary(shift => shift.Id);

        var filtered = assignments
            .Where(assignment => filterSet.IsWithinDateRange(assignment.AssignedDate.ToDateTime(TimeOnly.MinValue)))
            .Where(assignment => filterSet.Matches(FilterType.Employee, assignment.EmployeeId))
            .ToList();

        var rows = filtered
            .GroupBy(assignment => new { assignment.EmployeeId, assignment.EmployeeName })
            .Select(group =>
            {
                var totalHours = group.Sum(assignment => shiftsById.TryGetValue(assignment.ShiftId, out var shift) ? (shift.EndTime - shift.StartTime).TotalHours : 0);
                return new ReportRowDto(new Dictionary<string, string>
                {
                    ["employeeName"] = group.Key.EmployeeName,
                    ["shiftCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                    ["totalHours"] = totalHours.ToString("0.0", CultureInfo.InvariantCulture),
                });
            })
            .OrderByDescending(row => double.Parse(row.Values["totalHours"], CultureInfo.InvariantCulture))
            .ToList();

        var summary = new Dictionary<string, string> { ["کارمندان"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunBranchPerformanceAsync(ReportFilterSet filterSet, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);
        var organizations = await _organizationQueryService.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);

        var branchNamesById = new Dictionary<string, string>();
        foreach (var organization in organizations)
        {
            var branches = await _organizationQueryService.GetBranchesAsync(organization.Id, cancellationToken).ConfigureAwait(false);
            foreach (var branch in branches)
            {
                branchNamesById[branch.Id] = branch.Name;
            }
        }

        var filtered = bookings
            .Where(booking => filterSet.IsWithinDateRange(booking.ScheduledAt))
            .Where(booking => filterSet.Matches(FilterType.Branch, booking.BranchId))
            .ToList();

        var rows = filtered
            .GroupBy(booking => booking.BranchId)
            .OrderByDescending(group => group.Sum(booking => MoneyParser.Parse(booking.Price)))
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["branchName"] = branchNamesById.TryGetValue(group.Key, out var name) ? name : group.Key,
                ["bookingCount"] = group.Count().ToString(CultureInfo.InvariantCulture),
                ["revenue"] = FormatCurrency(group.Sum(booking => MoneyParser.Parse(booking.Price))),
            }))
            .ToList();

        var summary = new Dictionary<string, string> { ["شعبه‌ها"] = rows.Count.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
    }

    private async Task<(IReadOnlyList<ReportRowDto>, IReadOnlyDictionary<string, string>)> RunAiUsageSummaryAsync(CancellationToken cancellationToken)
    {
        var usage = await _tokenUsageTracker.GetUsageHistoryAsync(cancellationToken).ConfigureAwait(false);

        var rows = usage
            .GroupBy(record => record.ProviderType)
            .OrderByDescending(group => group.Sum(record => record.TotalTokens))
            .Select(group => new ReportRowDto(new Dictionary<string, string>
            {
                ["provider"] = group.Key.ToString(),
                ["sessionCount"] = group.Select(record => record.SessionId).Distinct().Count().ToString(CultureInfo.InvariantCulture),
                ["totalTokens"] = group.Sum(record => record.TotalTokens).ToString(CultureInfo.InvariantCulture),
            }))
            .ToList();

        var totalTokens = await _tokenUsageTracker.GetTotalTokensAsync(cancellationToken).ConfigureAwait(false);
        var summary = new Dictionary<string, string> { ["جمع توکن‌ها"] = totalTokens.ToString(CultureInfo.InvariantCulture) };
        return (rows, summary);
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
