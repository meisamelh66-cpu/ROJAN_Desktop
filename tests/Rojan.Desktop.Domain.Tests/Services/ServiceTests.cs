using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Domain.Tests.Services;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class ServiceTests
{
    private static Service MakeService(string id = "service-1") =>
        new(id, "Haircut & Style", ServiceCategory.Hair, ServiceStatus.Active, 60, "$65", "Classic cut and blow-dry finish.");

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var first = MakeService();
        var second = MakeService();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentStatus_AreNotEqual()
    {
        var first = MakeService() with { Status = ServiceStatus.Active };
        var second = MakeService() with { Status = ServiceStatus.Discontinued };

        Assert.NotEqual(first, second);
    }
}
