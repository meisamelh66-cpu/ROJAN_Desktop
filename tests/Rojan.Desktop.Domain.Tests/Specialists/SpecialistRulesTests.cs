using Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Domain.Tests.Specialists;

public sealed class SpecialistRulesTests
{
    [Theory]
    [InlineData(SpecialistStatus.Active, SpecialistStatus.OnLeave, true)]
    [InlineData(SpecialistStatus.Active, SpecialistStatus.Inactive, true)]
    [InlineData(SpecialistStatus.Active, SpecialistStatus.Active, false)]
    [InlineData(SpecialistStatus.OnLeave, SpecialistStatus.Active, true)]
    [InlineData(SpecialistStatus.OnLeave, SpecialistStatus.Inactive, true)]
    [InlineData(SpecialistStatus.OnLeave, SpecialistStatus.OnLeave, false)]
    [InlineData(SpecialistStatus.Inactive, SpecialistStatus.Active, true)]
    [InlineData(SpecialistStatus.Inactive, SpecialistStatus.OnLeave, false)]
    [InlineData(SpecialistStatus.Inactive, SpecialistStatus.Inactive, false)]
    public void IsValidTransition_VariousPairs_MatchesExpectedLifecycle(SpecialistStatus from, SpecialistStatus to, bool expected)
    {
        Assert.Equal(expected, SpecialistRules.IsValidTransition(from, to));
    }

    [Fact]
    public void IsValidTransition_EveryStatus_HasAtLeastOneWayOut()
    {
        // Unlike BookingRules, no SpecialistStatus is fully terminal - an employment relationship is
        // ongoing, not a one-shot event, so every status (including Inactive/archived) must be able
        // to transition somewhere.
        foreach (var status in Enum.GetValues<SpecialistStatus>())
        {
            var hasAnyValidTransition = Enum.GetValues<SpecialistStatus>()
                .Any(target => SpecialistRules.IsValidTransition(status, target));

            Assert.True(hasAnyValidTransition, $"{status} should have at least one valid outgoing transition.");
        }
    }

    [Theory]
    [InlineData(SpecialistStatus.OnLeave, true)]
    [InlineData(SpecialistStatus.Inactive, true)]
    [InlineData(SpecialistStatus.Active, false)]
    public void CanActivate_VariousStatuses_MatchesExpectedLifecycle(SpecialistStatus from, bool expected)
    {
        Assert.Equal(expected, SpecialistRules.CanActivate(from));
    }

    [Theory]
    [InlineData(SpecialistStatus.Active, true)]
    [InlineData(SpecialistStatus.OnLeave, false)]
    [InlineData(SpecialistStatus.Inactive, false)]
    public void CanDeactivate_VariousStatuses_MatchesExpectedLifecycle(SpecialistStatus from, bool expected)
    {
        Assert.Equal(expected, SpecialistRules.CanDeactivate(from));
    }

    [Theory]
    [InlineData(SpecialistStatus.Active, true)]
    [InlineData(SpecialistStatus.OnLeave, true)]
    [InlineData(SpecialistStatus.Inactive, false)]
    public void CanArchive_VariousStatuses_MatchesExpectedLifecycle(SpecialistStatus from, bool expected)
    {
        Assert.Equal(expected, SpecialistRules.CanArchive(from));
    }

    [Theory]
    [InlineData(SpecialistStatus.Inactive, true)]
    [InlineData(SpecialistStatus.Active, false)]
    [InlineData(SpecialistStatus.OnLeave, false)]
    public void IsArchived_VariousStatuses_MatchesExpectedLifecycle(SpecialistStatus status, bool expected)
    {
        Assert.Equal(expected, SpecialistRules.IsArchived(status));
    }

    [Fact]
    public void CanActivate_AndIsValidTransitionToActive_AgreeForEveryNonActiveStatus()
    {
        // CanActivate is a named convenience over IsValidTransition(from, Active) - guard against the
        // two ever silently drifting apart as the transition table evolves.
        foreach (var status in Enum.GetValues<SpecialistStatus>().Where(status => status != SpecialistStatus.Active))
        {
            Assert.Equal(SpecialistRules.IsValidTransition(status, SpecialistStatus.Active), SpecialistRules.CanActivate(status));
        }
    }

    [Fact]
    public void CanArchive_AndIsValidTransitionToInactive_AgreeForEveryNonInactiveStatus()
    {
        foreach (var status in Enum.GetValues<SpecialistStatus>().Where(status => status != SpecialistStatus.Inactive))
        {
            Assert.Equal(SpecialistRules.IsValidTransition(status, SpecialistStatus.Inactive), SpecialistRules.CanArchive(status));
        }
    }
}
