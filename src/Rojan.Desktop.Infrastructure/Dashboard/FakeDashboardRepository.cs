using Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Infrastructure.Dashboard;

/// <summary>
/// In-memory <see cref="IDashboardRepository"/> providing static sample
/// data - Phase 06B explicitly has no backend integration yet. The small
/// artificial delay is deliberate: without it, every load resolves within
/// the same UI frame and the ViewModel's <c>Loading</c> state would never
/// actually be observable, which would leave that state effectively
/// untested by simply running the app.
/// </summary>
public sealed class FakeDashboardRepository : IDashboardRepository
{
    private static readonly IReadOnlyList<KpiMetric> Metrics =
    [
        new KpiMetric("kpi-bookings", "مجموع نوبت‌ها", "128", TrendDirection.Up, 12.5),
        new KpiMetric("kpi-clients", "مشتریان فعال", "42", TrendDirection.Up, 4.2),
        new KpiMetric("kpi-revenue", "درآمد (ماه جاری)", "124,000,000 تومان", TrendDirection.Down, 2.1),
        new KpiMetric("kpi-tasks", "کارهای در انتظار", "7", TrendDirection.Flat, 0),
    ];

    public async Task<IReadOnlyList<KpiMetric>> GetKpiMetricsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return Metrics;
    }

    public async Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        var now = DateTimeOffset.Now;
        return
        [
            new ActivityEntry("activity-1", "نوبت جدید ثبت شد", now.AddMinutes(-2)),
            new ActivityEntry("activity-2", "پروفایل مشتری به‌روزرسانی شد", now.AddHours(-1)),
            new ActivityEntry("activity-3", "پرداخت دریافت شد", now.AddHours(-3)),
            new ActivityEntry("activity-4", "کار تکمیل شد", now.AddDays(-1)),
        ];
    }
}
