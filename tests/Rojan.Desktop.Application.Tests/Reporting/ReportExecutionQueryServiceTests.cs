using Rojan.Desktop.Application.Reporting;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppAI = Rojan.Desktop.Application.AI;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppOrganizations = Rojan.Desktop.Application.Organizations;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportExecutionQueryServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 7, 19, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<AppCustomers.CustomerDto> Customers =
    [
        new("customer-1", "Alice Adams", "Acme Co", "alice@example.com", "555-0001", AppCustomers.CustomerStatus.Active, "$1,200", Today.AddDays(-2), string.Empty, "org-1", "branch-1"),
        new("customer-2", "Bob Baker", string.Empty, "bob@example.com", "555-0002", AppCustomers.CustomerStatus.Churned, "$300", Today.AddDays(-40), string.Empty, "org-1", "branch-1"),
        new("customer-3", "Cara Chen", string.Empty, "cara@example.com", "555-0003", AppCustomers.CustomerStatus.Vip, "$5,000", Today.AddDays(-1), string.Empty, "org-1", "branch-1"),
    ];

    private static readonly IReadOnlyList<AppBookings.BookingDto> Bookings =
    [
        new("booking-1", "customer-1", "Alice Adams", "service-1", "Haircut", "specialist-1", "Jordan Lee", Today.AddHours(-2), 60, "$65", AppBookings.BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
        new("booking-2", "customer-3", "Cara Chen", "service-2", "Colour", "specialist-1", "Jordan Lee", Today.AddHours(-1), 90, "$120", AppBookings.BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
        new("booking-3", "customer-2", "Bob Baker", "service-1", "Haircut", "specialist-2", "Priya Nair", Today.AddDays(-10), 60, "$65", AppBookings.BookingStatus.Cancelled, string.Empty, "org-1", "branch-1"),
    ];

    private static readonly IReadOnlyList<AppServices.ServiceDto> Services =
    [
        new("service-1", "Haircut", AppServices.ServiceCategory.Hair, AppServices.ServiceStatus.Active, 60, "$65", string.Empty),
        new("service-2", "Colour", AppServices.ServiceCategory.Colour, AppServices.ServiceStatus.Active, 90, "$120", string.Empty),
    ];

    private static readonly IReadOnlyList<AppSpecialists.SpecialistDto> Specialists =
    [
        new("specialist-1", "Jordan Lee", "Stylist", "jordan@example.com", "555-1001", AppSpecialists.SpecialistStatus.Active, string.Empty),
        new("specialist-2", "Priya Nair", "Colorist", "priya@example.com", "555-1002", AppSpecialists.SpecialistStatus.Active, string.Empty),
    ];

    private static readonly IReadOnlyList<AppInventory.ProductDto> Products =
    [
        new("product-1", "SKU-1", "Shampoo", "cat-1", "Hair Care", "supplier-1", "Acme Supply", "$20", AppInventory.ProductStatus.Active, string.Empty, "org-1", "branch-1"),
        new("product-2", "SKU-2", "Conditioner", "cat-1", "Hair Care", "supplier-1", "Acme Supply", "$18", AppInventory.ProductStatus.Active, string.Empty, "org-1", "branch-1"),
    ];

    private static readonly IReadOnlyList<AppInventory.InventoryItemDto> InventoryItems =
    [
        new("item-1", "product-1", "Shampoo", 50, 10),
        new("item-2", "product-2", "Conditioner", 2, 10),
    ];

    private static readonly IReadOnlyList<AppAccounting.InvoiceDto> Invoices =
    [
        new("invoice-1", "customer-1", "Alice Adams", "booking-1", "BK-1", Today.AddHours(-2), AppAccounting.InvoiceStatus.Paid, 65m, 0m, 65m, string.Empty),
        new("invoice-2", "customer-3", "Cara Chen", "booking-2", "BK-2", Today.AddHours(-1), AppAccounting.InvoiceStatus.Paid, 120m, 0m, 120m, string.Empty),
        new("invoice-3", "customer-2", "Bob Baker", "booking-3", "BK-3", Today.AddDays(-10), AppAccounting.InvoiceStatus.Cancelled, 65m, 0m, 65m, string.Empty),
    ];

    private static readonly IReadOnlyList<AppHr.EmployeeDto> Employees =
    [
        new("employee-1", "specialist-1", "Jordan Lee", "jordan@example.com", "555-1001", AppHr.EmployeeRole.Stylist, AppHr.Department.Hair, AppHr.EmploymentType.FullTime, AppHr.EmployeeStatus.Active, new DateOnly(2021, 1, 1), 3000m),
        new("employee-2", "specialist-2", "Priya Nair", "priya@example.com", "555-1002", AppHr.EmployeeRole.Colorist, AppHr.Department.Hair, AppHr.EmploymentType.FullTime, AppHr.EmployeeStatus.Active, new DateOnly(2021, 1, 1), 2900m),
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AppHr.AttendanceDto>> AttendanceByEmployee =
        new Dictionary<string, IReadOnlyList<AppHr.AttendanceDto>>
        {
            ["employee-1"] =
            [
                new("attendance-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(Today.Date), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), AppHr.AttendanceStatus.Present, string.Empty),
                new("attendance-2", "employee-1", "Jordan Lee", DateOnly.FromDateTime(Today.AddDays(-1).Date), new TimeSpan(9, 15, 0), new TimeSpan(17, 0, 0), AppHr.AttendanceStatus.Late, string.Empty),
            ],
            ["employee-2"] =
            [
                new("attendance-3", "employee-2", "Priya Nair", DateOnly.FromDateTime(Today.Date), null, null, AppHr.AttendanceStatus.Absent, string.Empty),
            ],
        };

    private static readonly IReadOnlyList<AppHr.CommissionTransactionDto> CommissionTransactions =
    [
        new("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, Today.AddHours(-2)),
        new("commission-2", "employee-1", "Jordan Lee", "invoice-2", "Colour", 120m, 12m, Today.AddHours(-1)),
    ];

    private static readonly IReadOnlyList<AppHr.PayrollSummaryDto> PayrollSummaries =
    [
        new("payroll-1", "employee-1", "Jordan Lee", Today.Month, Today.Year, 3000m, 18.5m, 0m, 0m, 3018.5m, Today),
        new("payroll-2", "employee-2", "Priya Nair", Today.Month, Today.Year, 2900m, 0m, 100m, 50m, 2950m, Today),
    ];

    private static readonly IReadOnlyList<AppInventory.StockTransactionDto> Transactions =
    [
        new("txn-1", "product-1", "Shampoo", AppInventory.StockTransactionType.Received, 20, Today.AddDays(-2), string.Empty),
        new("txn-2", "product-2", "Conditioner", AppInventory.StockTransactionType.Received, 10, Today.AddDays(-1), string.Empty),
        new("txn-3", "product-1", "Shampoo", AppInventory.StockTransactionType.Sold, 4, Today, string.Empty),
    ];

    private static readonly IReadOnlyList<AppHr.ShiftDto> Shifts =
    [
        new("shift-1", "Morning", AppHr.Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
    ];

    private static readonly IReadOnlyList<AppHr.ShiftAssignmentDto> ShiftAssignments =
    [
        new("assignment-1", "shift-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(Today.Date)),
        new("assignment-2", "shift-1", "employee-2", "Priya Nair", DateOnly.FromDateTime(Today.Date)),
    ];

    private static readonly IReadOnlyList<AppOrganizations.OrganizationDto> Organizations =
    [
        new("org-1", "Acme Co", "Acme Co LLC", string.Empty, "#000000", "TIN-1", AppOrganizations.SubscriptionPlan.Enterprise, AppOrganizations.OrganizationStatus.Active, Today, "AC", "555-0000", "info@acme.example", "Address", "UTC", "en", "USD"),
    ];

    private static readonly IReadOnlyList<AppOrganizations.BranchDto> Branches =
    [
        new("branch-1", "org-1", "Main Branch", "MB-01", "Address", "555-0001", "branch@acme.example", "Manager", "UTC", "USD", AppOrganizations.BranchStatus.Active),
    ];

    private static readonly IReadOnlyList<AppAI.TokenUsageRecordDto> TokenUsage =
    [
        new("usage-1", "session-1", AppAI.AIProviderType.Mock, 8, 24, 32, Today.AddHours(-1)),
        new("usage-2", "session-2", AppAI.AIProviderType.Mock, 6, 20, 26, Today.AddHours(-2)),
    ];

    private static ReportExecutionQueryService CreateSut() => new(
        new StubReportingRepository(),
        new StubCustomerQueryService(Customers),
        new StubBookingQueryService(Bookings),
        new StubServiceQueryService(Services),
        new StubProductQueryService(Products),
        new StubInventoryQueryService(InventoryItems, transactions: Transactions),
        new StubInvoiceQueryService(Invoices),
        new StubEmployeeQueryService(Employees),
        new StubAttendanceQueryService(AttendanceByEmployee),
        new StubCommissionQueryService(CommissionTransactions),
        new StubPayrollQueryService(PayrollSummaries),
        new StubShiftQueryService(ShiftAssignments, Shifts),
        new StubOrganizationQueryService(Organizations, Branches),
        new StubTokenUsageTracker(TokenUsage));

    [Fact]
    public async Task RunReportAsync_WithUnknownReportId_Throws()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunReportAsync("does-not-exist", []));
    }

    [Fact]
    public async Task RunReportAsync_RevenueReport_GroupsPaidAndUnpaidInvoicesByDay()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("revenue-report", []);

        Assert.Equal("Revenue Report", result.ReportName);
        Assert.NotEmpty(result.Rows);
        var totalInvoices = result.Rows.Sum(row => int.Parse(row.Values["invoiceCount"], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(3, totalInvoices);
        Assert.Equal("250 تومان", result.Summary["درآمد کل"]);
    }

    [Fact]
    public async Task RunReportAsync_RevenueReport_WithDateRangeFilter_ExcludesOutOfRangeInvoices()
    {
        var sut = CreateSut();
        var filters = new[] { new ReportFilterDto(FilterType.DateRange, $"{Today.AddDays(-1):O}|{Today.AddDays(1):O}", "range") };

        var result = await sut.RunReportAsync("revenue-report", filters);

        Assert.Equal("185 تومان", result.Summary["درآمد کل"]);
    }

    [Fact]
    public async Task RunReportAsync_SalesReport_ListsEveryInvoiceWithCorrectTotal()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("sales-report", []);

        Assert.Equal(3, result.Rows.Count);
        Assert.Contains(result.Rows, row => row.Values["invoiceId"] == "invoice-1" && row.Values["total"] == "65 تومان");
    }

    [Fact]
    public async Task RunReportAsync_SalesReport_WithStatusFilter_OnlyReturnsMatchingInvoices()
    {
        var sut = CreateSut();
        var filters = new[] { new ReportFilterDto(FilterType.Status, "Cancelled", "status") };

        var result = await sut.RunReportAsync("sales-report", filters);

        Assert.Single(result.Rows);
        Assert.Equal("invoice-3", result.Rows[0].Values["invoiceId"]);
    }

    [Fact]
    public async Task RunReportAsync_AppointmentsReport_ListsEveryBooking()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("appointments-report", []);

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal("3", result.Summary["نوبت‌ها"]);
    }

    [Fact]
    public async Task RunReportAsync_AppointmentsReport_WithSpecialistFilter_OnlyReturnsThatSpecialistsBookings()
    {
        var sut = CreateSut();
        var filters = new[] { new ReportFilterDto(FilterType.Specialist, "specialist-2", "specialist") };

        var result = await sut.RunReportAsync("appointments-report", filters);

        Assert.Single(result.Rows);
        Assert.Equal("Priya Nair", result.Rows[0].Values["specialistName"]);
    }

    [Fact]
    public async Task RunReportAsync_CustomerRetention_GroupsCustomersByStatusWithCorrectPercentages()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("customer-retention", []);

        Assert.Equal(3, result.Rows.Count);
        var churnedRow = result.Rows.Single(row => row.Values["status"] == "Churned");
        Assert.Equal("1", churnedRow.Values["count"]);
        Assert.Equal("33.3%", churnedRow.Values["percentage"]);
    }

    [Fact]
    public async Task RunReportAsync_ServicePopularity_GroupsBookingsByServiceWithRevenue()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("service-popularity", []);

        Assert.Equal(2, result.Rows.Count);
        var haircutRow = result.Rows.Single(row => row.Values["serviceName"] == "Haircut");
        Assert.Equal("2", haircutRow.Values["bookingCount"]);
        Assert.Equal("130 تومان", haircutRow.Values["revenue"]);
    }

    [Fact]
    public async Task RunReportAsync_SpecialistPerformance_IncludesCommissionEarned()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("specialist-performance", []);

        var jordanRow = result.Rows.Single(row => row.Values["specialistName"] == "Jordan Lee");
        Assert.Equal("2", jordanRow.Values["bookingCount"]);
        Assert.Equal("19 تومان", jordanRow.Values["commissionEarned"]);
    }

    [Fact]
    public async Task RunReportAsync_InventoryValuation_ComputesQuantityTimesUnitPrice()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("inventory-valuation", []);

        var shampooRow = result.Rows.Single(row => row.Values["productName"] == "Shampoo");
        Assert.Equal("1,000 تومان", shampooRow.Values["totalValue"]);
        // Shampoo (50 x $20 = $1,000) + Conditioner (2 x $18 = $36).
        Assert.Equal("1,036 تومان", result.Summary["ارزش کل"]);
    }

    [Fact]
    public async Task RunReportAsync_LowStock_OnlyReturnsItemsAtOrBelowReorderThreshold()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("low-stock", []);

        Assert.Single(result.Rows);
        Assert.Equal("Conditioner", result.Rows[0].Values["productName"]);
        Assert.Equal("8", result.Rows[0].Values["shortfall"]);
    }

    [Fact]
    public async Task RunReportAsync_PayrollSummary_ListsEveryEmployeeWithNetSalary()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("payroll-summary", []);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("5,969 تومان", result.Summary["جمع خالص حقوق"]);
    }

    [Fact]
    public async Task RunReportAsync_CommissionSummary_GroupsTransactionsByEmployee()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("commission-summary", []);

        Assert.Single(result.Rows);
        Assert.Equal("Jordan Lee", result.Rows[0].Values["employeeName"]);
        Assert.Equal("2", result.Rows[0].Values["transactionCount"]);
        Assert.Equal("19 تومان", result.Rows[0].Values["totalCommission"]);
    }

    [Fact]
    public async Task RunReportAsync_AttendanceSummary_ComputesPresentRateExcludingEmptyRecords()
    {
        var sut = CreateSut();
        var filters = new[] { new ReportFilterDto(FilterType.DateRange, $"{Today.AddDays(-2):O}|{Today.AddDays(1):O}", "range") };

        var result = await sut.RunReportAsync("attendance-summary", filters);

        var jordanRow = result.Rows.Single(row => row.Values["employeeName"] == "Jordan Lee");
        Assert.Equal("1", jordanRow.Values["presentCount"]);
        Assert.Equal("1", jordanRow.Values["lateCount"]);
        Assert.Equal("50.0%", jordanRow.Values["attendanceRate"]);
    }

    [Fact]
    public async Task RunReportAsync_DailyDashboard_ReturnsMetricValuePairsCoveringEveryModule()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("daily-dashboard", []);

        Assert.Equal(11, result.Rows.Count);
        Assert.Contains(result.Rows, row => row.Values["metric"] == "درآمد کل");
        Assert.Contains(result.Rows, row => row.Values["metric"] == "اقلام رو به اتمام" && row.Values["value"] == "1");
    }

    [Fact]
    public async Task RunReportAsync_CashFlow_SumsOnlyPaidAndPartiallyPaidInvoices()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("cash-flow", []);

        Assert.NotEmpty(result.Rows);
        Assert.Contains("جریان نقدی خالص", result.Summary.Keys);
    }

    [Fact]
    public async Task RunReportAsync_OutstandingPayments_ExcludesPaidAndCancelledInvoices()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("outstanding-payments", []);

        Assert.DoesNotContain(result.Rows, row => row.Values["invoiceId"] == "invoice-1");
        Assert.DoesNotContain(result.Rows, row => row.Values["invoiceId"] == "invoice-3");
    }

    [Fact]
    public async Task RunReportAsync_TaxSummary_GroupsByMonth()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("tax-summary", []);

        Assert.NotEmpty(result.Rows);
        Assert.Contains("جمع مالیات", result.Summary.Keys);
    }

    [Fact]
    public async Task RunReportAsync_VipCustomers_OnlyReturnsVipStatusCustomers()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("vip-customers", []);

        Assert.Single(result.Rows);
        Assert.Equal("Cara Chen", result.Rows[0].Values["name"]);
    }

    [Fact]
    public async Task RunReportAsync_InactiveCustomers_OnlyReturnsInactiveStatusCustomers()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("inactive-customers", []);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task RunReportAsync_CustomerLifetimeValue_ListsEveryCustomerOrderedByValue()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("customer-lifetime-value", []);

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal("Cara Chen", result.Rows[0].Values["name"]);
    }

    [Fact]
    public async Task RunReportAsync_AppointmentStatusBreakdown_GroupsBookingsByStatus()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("appointment-status-breakdown", []);

        Assert.NotEmpty(result.Rows);
        Assert.Contains("نرخ لغو", result.Summary.Keys);
    }

    [Fact]
    public async Task RunReportAsync_PeakHours_GroupsBookingsByHourOfDay()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("peak-hours", []);

        Assert.NotEmpty(result.Rows);
        Assert.Contains("شلوغ‌ترین روز", result.Summary.Keys);
    }

    [Fact]
    public async Task RunReportAsync_InventoryMovements_ListsEveryTransaction()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("inventory-movements", []);

        Assert.Equal(3, result.Rows.Count);
    }

    [Fact]
    public async Task RunReportAsync_SupplierPurchases_OnlyCountsReceivedTransactions()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("supplier-purchases", []);

        Assert.All(result.Rows, row => Assert.True(int.Parse(row.Values["totalQuantity"], System.Globalization.CultureInfo.InvariantCulture) > 0));
    }

    [Fact]
    public async Task RunReportAsync_EmployeeWorkingHours_SumsShiftDurationsPerEmployee()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("employee-working-hours", []);

        Assert.Equal(2, result.Rows.Count);
        Assert.Contains(result.Rows, row => row.Values["employeeName"] == "Jordan Lee" && row.Values["totalHours"] == "8.0");
    }

    [Fact]
    public async Task RunReportAsync_BranchPerformance_ResolvesBranchNamesAndAggregatesRevenue()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("branch-performance", []);

        Assert.NotEmpty(result.Rows);
        Assert.Contains(result.Rows, row => row.Values["branchName"] == "Main Branch");
    }

    [Fact]
    public async Task RunReportAsync_AiUsageSummary_GroupsTokenUsageByProvider()
    {
        var sut = CreateSut();

        var result = await sut.RunReportAsync("ai-usage-summary", []);

        Assert.Single(result.Rows);
        Assert.Equal("58", result.Rows[0].Values["totalTokens"]);
        Assert.Equal("58", result.Summary["جمع توکن‌ها"]);
    }
}
