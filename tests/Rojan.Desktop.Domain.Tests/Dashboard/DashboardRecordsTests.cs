using Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Domain.Tests.Dashboard;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests.</summary>
public sealed class DashboardRecordsTests
{
    [Fact]
    public void KpiMetric_SameValues_AreEqual()
    {
        var first = new KpiMetric("kpi-1", "Total Bookings", "128", TrendDirection.Up, 12.5);
        var second = new KpiMetric("kpi-1", "Total Bookings", "128", TrendDirection.Up, 12.5);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ActivityEntry_DifferentDescription_AreNotEqual()
    {
        var occurredAt = DateTimeOffset.UnixEpoch;
        var first = new ActivityEntry("activity-1", "New booking created", occurredAt);
        var second = new ActivityEntry("activity-1", "Payment received", occurredAt);

        Assert.NotEqual(first, second);
    }
}
