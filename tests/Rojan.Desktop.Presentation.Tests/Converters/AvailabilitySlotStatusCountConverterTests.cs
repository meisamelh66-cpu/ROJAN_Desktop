using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Presentation.Converters;

namespace Rojan.Desktop.Presentation.Tests.Converters;

public sealed class AvailabilitySlotStatusCountConverterTests
{
    private static AvailabilitySlotDto MakeSlot(AvailabilityStatus status) =>
        new("specialist-1", "Jordan Lee", DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(30), status);

    [Fact]
    public void Convert_CountsOnlyMatchingStatus()
    {
        var sut = new AvailabilitySlotStatusCountConverter();
        IReadOnlyList<AvailabilitySlotDto> slots =
        [
            MakeSlot(AvailabilityStatus.Available),
            MakeSlot(AvailabilityStatus.Available),
            MakeSlot(AvailabilityStatus.Booked),
            MakeSlot(AvailabilityStatus.Unavailable),
        ];

        var result = sut.Convert(slots, typeof(int), "Available", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(2, result);
    }

    [Fact]
    public void Convert_NoMatchingSlots_ReturnsZero()
    {
        var sut = new AvailabilitySlotStatusCountConverter();
        IReadOnlyList<AvailabilitySlotDto> slots = [MakeSlot(AvailabilityStatus.Available)];

        var result = sut.Convert(slots, typeof(int), "Booked", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Convert_UnrecognizedParameter_ReturnsZero()
    {
        var sut = new AvailabilitySlotStatusCountConverter();
        IReadOnlyList<AvailabilitySlotDto> slots = [MakeSlot(AvailabilityStatus.Available)];

        var result = sut.Convert(slots, typeof(int), "NotAStatus", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Convert_ValueNotAList_ReturnsZero()
    {
        var sut = new AvailabilitySlotStatusCountConverter();

        var result = sut.Convert("not a list", typeof(int), "Available", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var sut = new AvailabilitySlotStatusCountConverter();

        Assert.Throws<NotSupportedException>(() =>
            sut.ConvertBack(2, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));
    }
}
