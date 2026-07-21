using Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Infrastructure.Reporting;

/// <summary>
/// In-memory <see cref="IReportingRepository"/>. Seeds the fifteen
/// system-defined <see cref="ReportDefinition"/>s (the report catalog) plus
/// a handful of <see cref="ReportSnapshot"/>s so "Saved Reports"/"Recent
/// Reports" have something to show on first launch. Registered as a
/// singleton (same reasoning as every other Fake repository with real
/// writes - <c>FakeHrRepository</c>, <c>FakeAccountingRepository</c> - it
/// must remember snapshot writes for the app's lifetime).
/// </summary>
public sealed class FakeReportingRepository : IReportingRepository
{
    private readonly List<ReportDefinition> _reportDefinitions;
    private readonly List<ReportSnapshot> _snapshots;

    public FakeReportingRepository()
    {
        _reportDefinitions =
        [
            new ReportDefinition(
                "revenue-report", "گزارش درآمد", "مجموع درآمد روزانه بر اساس فاکتورهای حسابداری.",
                ReportType.RevenueReport, ReportCategory.Financial, true,
                [
                    new ReportColumn("date", "تاریخ", ReportColumnDataType.Date),
                    new ReportColumn("invoiceCount", "تعداد فاکتور", ReportColumnDataType.Number),
                    new ReportColumn("totalRevenue", "درآمد کل", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "sales-report", "گزارش فروش", "تمام فاکتورهای صادرشده به همراه مشتری و وضعیت.",
                ReportType.SalesReport, ReportCategory.Financial, true,
                [
                    new ReportColumn("invoiceId", "فاکتور", ReportColumnDataType.Text),
                    new ReportColumn("customerName", "مشتری", ReportColumnDataType.Text),
                    new ReportColumn("issuedAt", "تاریخ صدور", ReportColumnDataType.Date),
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("total", "مبلغ کل", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Customer, FilterType.Status]),
            new ReportDefinition(
                "appointments-report", "گزارش نوبت‌ها", "تمام نوبت‌ها به همراه خدمت، متخصص و وضعیت.",
                ReportType.AppointmentsReport, ReportCategory.Operations, true,
                [
                    new ReportColumn("date", "تاریخ", ReportColumnDataType.Date),
                    new ReportColumn("customerName", "مشتری", ReportColumnDataType.Text),
                    new ReportColumn("serviceName", "خدمت", ReportColumnDataType.Text),
                    new ReportColumn("specialistName", "متخصص", ReportColumnDataType.Text),
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("price", "قیمت", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Specialist, FilterType.Service, FilterType.Status, FilterType.Customer]),
            new ReportDefinition(
                "customer-growth", "رشد مشتریان", "مشتریان جدید به تفکیک ماه، بر اساس تاریخ آخرین تماس.",
                ReportType.CustomerGrowth, ReportCategory.Customers, true,
                [
                    new ReportColumn("period", "دوره", ReportColumnDataType.Text),
                    new ReportColumn("newCustomers", "مشتریان جدید", ReportColumnDataType.Number),
                    new ReportColumn("totalCustomers", "تعداد کل مشتریان", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "customer-retention", "نگهداشت مشتریان", "تعداد و سهم مشتریان به تفکیک وضعیت ارتباط.",
                ReportType.CustomerRetention, ReportCategory.Customers, true,
                [
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("count", "تعداد", ReportColumnDataType.Number),
                    new ReportColumn("percentage", "سهم", ReportColumnDataType.Percentage),
                ],
                [FilterType.Status]),
            new ReportDefinition(
                "service-popularity", "محبوبیت خدمات", "تعداد نوبت و درآمد به تفکیک خدمت.",
                ReportType.ServicePopularity, ReportCategory.Operations, true,
                [
                    new ReportColumn("serviceName", "خدمت", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "تعداد نوبت", ReportColumnDataType.Number),
                    new ReportColumn("revenue", "درآمد", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Service, FilterType.Category]),
            new ReportDefinition(
                "specialist-performance", "عملکرد متخصصان", "نوبت‌ها، درآمد و کمیسیون کسب‌شده به تفکیک متخصص.",
                ReportType.SpecialistPerformance, ReportCategory.Workforce, true,
                [
                    new ReportColumn("specialistName", "متخصص", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "تعداد نوبت", ReportColumnDataType.Number),
                    new ReportColumn("revenue", "درآمد", ReportColumnDataType.Currency),
                    new ReportColumn("commissionEarned", "کمیسیون کسب‌شده", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Specialist]),
            new ReportDefinition(
                "inventory-valuation", "ارزش‌گذاری موجودی", "مقدار و ارزش موجود به تفکیک محصول.",
                ReportType.InventoryValuation, ReportCategory.Operations, true,
                [
                    new ReportColumn("productName", "محصول", ReportColumnDataType.Text),
                    new ReportColumn("quantityOnHand", "موجودی", ReportColumnDataType.Number),
                    new ReportColumn("unitPrice", "قیمت واحد", ReportColumnDataType.Currency),
                    new ReportColumn("totalValue", "ارزش کل", ReportColumnDataType.Currency),
                ],
                [FilterType.Category]),
            new ReportDefinition(
                "low-stock", "کمبود موجودی", "محصولاتی که در آستانه یا زیر سطح سفارش مجدد هستند.",
                ReportType.LowStock, ReportCategory.Operations, true,
                [
                    new ReportColumn("productName", "محصول", ReportColumnDataType.Text),
                    new ReportColumn("quantityOnHand", "موجودی", ReportColumnDataType.Number),
                    new ReportColumn("reorderThreshold", "آستانه سفارش مجدد", ReportColumnDataType.Number),
                    new ReportColumn("shortfall", "کسری", ReportColumnDataType.Number),
                ],
                []),
            new ReportDefinition(
                "payroll-summary", "خلاصه حقوق و دستمزد", "حقوق پایه، کمیسیون، پاداش، کسورات و خالص پرداختی به تفکیک کارمند.",
                ReportType.PayrollSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "کارمند", ReportColumnDataType.Text),
                    new ReportColumn("period", "دوره", ReportColumnDataType.Text),
                    new ReportColumn("baseSalary", "حقوق پایه", ReportColumnDataType.Currency),
                    new ReportColumn("commissionTotal", "کمیسیون", ReportColumnDataType.Currency),
                    new ReportColumn("bonus", "پاداش", ReportColumnDataType.Currency),
                    new ReportColumn("deduction", "کسورات", ReportColumnDataType.Currency),
                    new ReportColumn("netSalary", "خالص پرداختی", ReportColumnDataType.Currency),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "commission-summary", "خلاصه کمیسیون", "تراکنش‌ها و جمع کمیسیون به تفکیک کارمند.",
                ReportType.CommissionSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "کارمند", ReportColumnDataType.Text),
                    new ReportColumn("transactionCount", "تعداد تراکنش", ReportColumnDataType.Number),
                    new ReportColumn("totalCommission", "جمع کمیسیون", ReportColumnDataType.Currency),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "attendance-summary", "خلاصه حضور و غیاب", "تعداد حضور، تأخیر و غیبت و نرخ حضور به تفکیک کارمند.",
                ReportType.AttendanceSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "کارمند", ReportColumnDataType.Text),
                    new ReportColumn("presentCount", "حاضر", ReportColumnDataType.Number),
                    new ReportColumn("lateCount", "تأخیر", ReportColumnDataType.Number),
                    new ReportColumn("absentCount", "غایب", ReportColumnDataType.Number),
                    new ReportColumn("attendanceRate", "نرخ حضور", ReportColumnDataType.Percentage),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "daily-dashboard", "داشبورد روزانه", "شاخص‌های کلیدی امروز در تمام ماژول‌ها.",
                ReportType.DailyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "شاخص", ReportColumnDataType.Text),
                    new ReportColumn("value", "مقدار", ReportColumnDataType.Text),
                ],
                []),
            new ReportDefinition(
                "weekly-dashboard", "داشبورد هفتگی", "شاخص‌های کلیدی این هفته در تمام ماژول‌ها.",
                ReportType.WeeklyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "شاخص", ReportColumnDataType.Text),
                    new ReportColumn("value", "مقدار", ReportColumnDataType.Text),
                ],
                []),
            new ReportDefinition(
                "monthly-dashboard", "داشبورد ماهانه", "شاخص‌های کلیدی این ماه در تمام ماژول‌ها.",
                ReportType.MonthlyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "شاخص", ReportColumnDataType.Text),
                    new ReportColumn("value", "مقدار", ReportColumnDataType.Text),
                ],
                []),

            // Phase 33: Enterprise Reporting & Business Intelligence Platform.
            new ReportDefinition(
                "cash-flow", "جریان نقدی", "مبالغ نقدی دریافت‌شده روزانه از فاکتورهای پرداخت‌شده.",
                ReportType.CashFlow, ReportCategory.Financial, true,
                [
                    new ReportColumn("date", "تاریخ", ReportColumnDataType.Date),
                    new ReportColumn("cashIn", "ورودی نقدی", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "outstanding-payments", "مطالبات معوق", "فاکتورهای صادرشده یا نیمه‌پرداخت‌شده که هنوز تسویه نشده‌اند.",
                ReportType.OutstandingPayments, ReportCategory.Financial, true,
                [
                    new ReportColumn("invoiceId", "فاکتور", ReportColumnDataType.Text),
                    new ReportColumn("customerName", "مشتری", ReportColumnDataType.Text),
                    new ReportColumn("issuedAt", "تاریخ صدور", ReportColumnDataType.Date),
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("outstanding", "مبلغ معوق", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Customer]),
            new ReportDefinition(
                "tax-summary", "خلاصه مالیات", "مبلغ مشمول مالیات و مالیات وصول‌شده به تفکیک ماه.",
                ReportType.TaxSummary, ReportCategory.Financial, true,
                [
                    new ReportColumn("period", "دوره", ReportColumnDataType.Text),
                    new ReportColumn("taxableAmount", "مبلغ مشمول مالیات", ReportColumnDataType.Currency),
                    new ReportColumn("taxCollected", "مالیات وصول‌شده", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "vip-customers", "مشتریان ویژه", "مشتریان با وضعیت ویژه، مرتب‌شده بر اساس ارزش کل خرید.",
                ReportType.VipCustomers, ReportCategory.Customers, true,
                [
                    new ReportColumn("name", "نام", ReportColumnDataType.Text),
                    new ReportColumn("company", "شرکت", ReportColumnDataType.Text),
                    new ReportColumn("lifetimeValue", "ارزش کل خرید", ReportColumnDataType.Currency),
                    new ReportColumn("lastContacted", "آخرین تماس", ReportColumnDataType.Date),
                ],
                []),
            new ReportDefinition(
                "inactive-customers", "مشتریان غیرفعال", "مشتریانی که مدت‌هاست تماسی از آن‌ها ثبت نشده.",
                ReportType.InactiveCustomers, ReportCategory.Customers, true,
                [
                    new ReportColumn("name", "نام", ReportColumnDataType.Text),
                    new ReportColumn("lastContacted", "آخرین تماس", ReportColumnDataType.Date),
                    new ReportColumn("phone", "تلفن", ReportColumnDataType.Text),
                ],
                []),
            new ReportDefinition(
                "customer-lifetime-value", "ارزش طول عمر مشتری", "تمام مشتریان مرتب‌شده بر اساس ارزش کل خرید.",
                ReportType.CustomerLifetimeValue, ReportCategory.Customers, true,
                [
                    new ReportColumn("name", "نام", ReportColumnDataType.Text),
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("lifetimeValue", "ارزش کل خرید", ReportColumnDataType.Currency),
                ],
                []),
            new ReportDefinition(
                "appointment-status-breakdown", "وضعیت نوبت‌ها", "تعداد و سهم نوبت‌ها به تفکیک وضعیت (تکمیل‌شده، لغوشده، عدم مراجعه و ...).",
                ReportType.AppointmentStatusBreakdown, ReportCategory.Operations, true,
                [
                    new ReportColumn("status", "وضعیت", ReportColumnDataType.Text),
                    new ReportColumn("count", "تعداد", ReportColumnDataType.Number),
                    new ReportColumn("percentage", "سهم", ReportColumnDataType.Percentage),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "peak-hours", "ساعات پرترافیک", "تعداد نوبت به تفکیک ساعت روز، برای شناسایی ساعات و روزهای شلوغ.",
                ReportType.PeakHours, ReportCategory.Operations, true,
                [
                    new ReportColumn("hour", "ساعت", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "تعداد نوبت", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "inventory-movements", "گردش موجودی انبار", "تمام تراکنش‌های ورود، فروش، مرجوعی و تعدیل موجودی.",
                ReportType.InventoryMovements, ReportCategory.Operations, true,
                [
                    new ReportColumn("date", "تاریخ", ReportColumnDataType.Date),
                    new ReportColumn("productName", "محصول", ReportColumnDataType.Text),
                    new ReportColumn("type", "نوع تراکنش", ReportColumnDataType.Text),
                    new ReportColumn("quantity", "مقدار", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "supplier-purchases", "خرید از تأمین‌کنندگان", "مقدار خرید دریافت‌شده به تفکیک تأمین‌کننده.",
                ReportType.SupplierPurchases, ReportCategory.Operations, true,
                [
                    new ReportColumn("supplierName", "تأمین‌کننده", ReportColumnDataType.Text),
                    new ReportColumn("transactionCount", "تعداد تراکنش", ReportColumnDataType.Number),
                    new ReportColumn("totalQuantity", "مجموع مقدار", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange, FilterType.Supplier]),
            new ReportDefinition(
                "employee-working-hours", "ساعات کاری کارمندان", "مجموع ساعات شیفت اختصاص‌یافته به هر کارمند.",
                ReportType.EmployeeWorkingHours, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "کارمند", ReportColumnDataType.Text),
                    new ReportColumn("shiftCount", "تعداد شیفت", ReportColumnDataType.Number),
                    new ReportColumn("totalHours", "مجموع ساعات", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange, FilterType.Employee]),
            new ReportDefinition(
                "branch-performance", "عملکرد شعبه‌ها", "تعداد نوبت و درآمد به تفکیک شعبه.",
                ReportType.BranchPerformance, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("branchName", "شعبه", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "تعداد نوبت", ReportColumnDataType.Number),
                    new ReportColumn("revenue", "درآمد", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Branch]),
            new ReportDefinition(
                "ai-usage-summary", "خلاصه مصرف هوش مصنوعی", "تعداد نشست و توکن مصرفی به تفکیک ارائه‌دهنده هوش مصنوعی.",
                ReportType.AiUsageSummary, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("provider", "ارائه‌دهنده", ReportColumnDataType.Text),
                    new ReportColumn("sessionCount", "تعداد نشست", ReportColumnDataType.Number),
                    new ReportColumn("totalTokens", "مجموع توکن", ReportColumnDataType.Number),
                ],
                []),
        ];

        var now = DateTimeOffset.Now;
        _snapshots =
        [
            new ReportSnapshot("snapshot-1", "revenue-report", "گزارش درآمد", now.AddDays(-1), [], 7, true),
            new ReportSnapshot("snapshot-2", "specialist-performance", "عملکرد متخصصان", now.AddDays(-1).AddHours(-2), [], 5, true),
            new ReportSnapshot("snapshot-3", "low-stock", "کمبود موجودی", now.AddHours(-5), [], 3, false),
            new ReportSnapshot("snapshot-4", "appointments-report", "گزارش نوبت‌ها", now.AddHours(-1), [], 12, false),
        ];
    }

    public Task<IReadOnlyList<ReportDefinition>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportDefinition>>(_reportDefinitions);

    public Task<ReportDefinition?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reportDefinitions.FirstOrDefault(definition => definition.Id == reportDefinitionId));

    public Task<IReadOnlyList<ReportSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportSnapshot>>(_snapshots.OrderByDescending(snapshot => snapshot.GeneratedAt).ToList());

    public Task<ReportSnapshot> CreateSnapshotAsync(ReportSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots.Add(snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<ReportSnapshot> UpdateSnapshotSavedStateAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        var index = _snapshots.FindIndex(snapshot => snapshot.Id == snapshotId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Snapshot '{snapshotId}' was not found.");
        }

        var updated = _snapshots[index] with { IsSaved = isSaved };
        _snapshots[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        _snapshots.RemoveAll(snapshot => snapshot.Id == snapshotId);
        return Task.CompletedTask;
    }
}
