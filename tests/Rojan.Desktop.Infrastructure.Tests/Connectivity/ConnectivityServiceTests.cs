using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Connectivity;

namespace Rojan.Desktop.Infrastructure.Tests.Connectivity;

/// <summary>Exercises the real <see cref="System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable"/>-backed check - assertions stay loose (a valid enum value, a defined relationship to the OS-reported state) since the test machine's actual network state is not something this suite controls.</summary>
public sealed class ConnectivityServiceTests
{
    [Fact]
    public void CurrentState_AfterConstruction_MatchesTheOsReportedAvailability()
    {
        using var service = new ConnectivityService();

        var expected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()
            ? ConnectionState.Online
            : ConnectionState.Offline;
        Assert.Equal(expected, service.CurrentState);
    }

    [Fact]
    public async Task CheckAsync_ReturnsTheSameValueAsCurrentStateAfterwards()
    {
        using var service = new ConnectivityService();

        var result = await service.CheckAsync();

        Assert.Equal(service.CurrentState, result);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var service = new ConnectivityService();

        var exception = Record.Exception(service.Dispose);

        Assert.Null(exception);
    }
}
