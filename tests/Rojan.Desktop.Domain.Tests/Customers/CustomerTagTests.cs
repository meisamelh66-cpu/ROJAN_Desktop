using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Domain.Tests.Customers;

/// <summary>Minimal smoke coverage - see the note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class CustomerTagTests
{
    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var createdAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerTag("tag-1", "customer-1", "VIP", createdAt);
        var second = new CustomerTag("tag-1", "customer-1", "VIP", createdAt);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentLabel_AreNotEqual()
    {
        var createdAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerTag("tag-1", "customer-1", "VIP", createdAt);
        var second = new CustomerTag("tag-1", "customer-1", "Regular", createdAt);

        Assert.NotEqual(first, second);
    }
}
