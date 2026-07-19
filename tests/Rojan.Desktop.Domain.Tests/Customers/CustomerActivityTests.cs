using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Domain.Tests.Customers;

/// <summary>Minimal smoke coverage - see the note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class CustomerActivityTests
{
    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var occurredAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerActivity("activity-1", "customer-1", "Customer created", occurredAt);
        var second = new CustomerActivity("activity-1", "customer-1", "Customer created", occurredAt);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentDescription_AreNotEqual()
    {
        var occurredAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerActivity("activity-1", "customer-1", "Customer created", occurredAt);
        var second = new CustomerActivity("activity-1", "customer-1", "Note added", occurredAt);

        Assert.NotEqual(first, second);
    }
}
