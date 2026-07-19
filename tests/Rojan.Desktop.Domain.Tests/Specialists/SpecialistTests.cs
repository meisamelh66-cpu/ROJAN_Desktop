using Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Domain.Tests.Specialists;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class SpecialistTests
{
    private static Specialist MakeSpecialist(string id = "specialist-1") =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            SpecialistStatus.Active, "Specializes in balayage.");

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var first = MakeSpecialist();
        var second = MakeSpecialist();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentStatus_AreNotEqual()
    {
        var first = MakeSpecialist() with { Status = SpecialistStatus.Active };
        var second = MakeSpecialist() with { Status = SpecialistStatus.OnLeave };

        Assert.NotEqual(first, second);
    }
}
