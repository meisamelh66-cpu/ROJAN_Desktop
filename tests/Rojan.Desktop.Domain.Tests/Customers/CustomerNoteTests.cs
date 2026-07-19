using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Domain.Tests.Customers;

/// <summary>Minimal smoke coverage - see the note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class CustomerNoteTests
{
    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var createdAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerNote("note-1", "customer-1", "Prefers evenings.", createdAt);
        var second = new CustomerNote("note-1", "customer-1", "Prefers evenings.", createdAt);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentText_AreNotEqual()
    {
        var createdAt = DateTimeOffset.UnixEpoch;
        var first = new CustomerNote("note-1", "customer-1", "Prefers evenings.", createdAt);
        var second = new CustomerNote("note-1", "customer-1", "Prefers mornings.", createdAt);

        Assert.NotEqual(first, second);
    }
}
