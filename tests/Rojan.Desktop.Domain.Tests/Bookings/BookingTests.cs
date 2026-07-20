using Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Domain.Tests.Bookings;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class BookingTests
{
    private static Booking MakeBooking(string id = "booking-1") =>
        new(id, string.Empty, "Amelia Hart", "service-2", "Colour Touch-Up", "specialist-1", "Jordan Lee",
            new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), 90, "$120", BookingStatus.Confirmed, "Notes", "org-1", "branch-1");

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var first = MakeBooking();
        var second = MakeBooking();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentStatus_AreNotEqual()
    {
        var first = MakeBooking() with { Status = BookingStatus.Pending };
        var second = MakeBooking() with { Status = BookingStatus.Cancelled };

        Assert.NotEqual(first, second);
    }
}
