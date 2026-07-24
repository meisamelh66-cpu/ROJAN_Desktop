using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Domain.Tests.Services;

public sealed class ServiceRulesTests
{
    [Theory]
    [InlineData(ServiceStatus.Active, ServiceStatus.Seasonal, true)]
    [InlineData(ServiceStatus.Active, ServiceStatus.Discontinued, true)]
    [InlineData(ServiceStatus.Active, ServiceStatus.Active, false)]
    [InlineData(ServiceStatus.Seasonal, ServiceStatus.Active, true)]
    [InlineData(ServiceStatus.Seasonal, ServiceStatus.Discontinued, true)]
    [InlineData(ServiceStatus.Seasonal, ServiceStatus.Seasonal, false)]
    [InlineData(ServiceStatus.Discontinued, ServiceStatus.Active, true)]
    [InlineData(ServiceStatus.Discontinued, ServiceStatus.Seasonal, false)]
    [InlineData(ServiceStatus.Discontinued, ServiceStatus.Discontinued, false)]
    public void IsValidTransition_VariousPairs_MatchesExpectedLifecycle(ServiceStatus from, ServiceStatus to, bool expected)
    {
        Assert.Equal(expected, ServiceRules.IsValidTransition(from, to));
    }

    [Fact]
    public void IsValidTransition_EveryStatus_HasAtLeastOneWayOut()
    {
        // Unlike BookingRules, no ServiceStatus is fully terminal - a catalog service is an ongoing
        // offering, not a one-shot event, so every status (including Discontinued) must be able to
        // transition somewhere.
        foreach (var status in Enum.GetValues<ServiceStatus>())
        {
            var hasAnyValidTransition = Enum.GetValues<ServiceStatus>()
                .Any(target => ServiceRules.IsValidTransition(status, target));

            Assert.True(hasAnyValidTransition, $"{status} should have at least one valid outgoing transition.");
        }
    }

    [Theory]
    [InlineData(ServiceStatus.Seasonal, true)]
    [InlineData(ServiceStatus.Discontinued, true)]
    [InlineData(ServiceStatus.Active, false)]
    public void CanActivate_VariousStatuses_MatchesExpectedLifecycle(ServiceStatus from, bool expected)
    {
        Assert.Equal(expected, ServiceRules.CanActivate(from));
    }

    [Theory]
    [InlineData(ServiceStatus.Active, true)]
    [InlineData(ServiceStatus.Seasonal, false)]
    [InlineData(ServiceStatus.Discontinued, false)]
    public void CanDeactivate_VariousStatuses_MatchesExpectedLifecycle(ServiceStatus from, bool expected)
    {
        Assert.Equal(expected, ServiceRules.CanDeactivate(from));
    }

    [Fact]
    public void CanActivate_AndIsValidTransitionToActive_AgreeForEveryNonActiveStatus()
    {
        // CanActivate is a named convenience over IsValidTransition(from, Active) - guard against the
        // two ever silently drifting apart as the transition table evolves.
        foreach (var status in Enum.GetValues<ServiceStatus>().Where(status => status != ServiceStatus.Active))
        {
            Assert.Equal(ServiceRules.IsValidTransition(status, ServiceStatus.Active), ServiceRules.CanActivate(status));
        }
    }
}
