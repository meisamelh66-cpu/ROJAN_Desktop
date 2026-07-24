using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Domain.Tests.Authentication;

public sealed class BranchRulesTests
{
    [Theory]
    [InlineData(BranchStatus.Active, BranchStatus.Inactive)]
    [InlineData(BranchStatus.Inactive, BranchStatus.Active)]
    public void IsValidTransition_AllowedTransition_ReturnsTrue(BranchStatus from, BranchStatus to)
    {
        Assert.True(BranchRules.IsValidTransition(from, to));
    }

    [Fact]
    public void Branch_ActiveStatus_IsActiveReturnsTrue()
    {
        var branch = new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow);

        Assert.True(branch.IsActive);
    }

    [Fact]
    public void Branch_InactiveStatus_IsActiveReturnsFalse()
    {
        var branch = new Branch("branch-1", "org-1", "Downtown", BranchStatus.Inactive, DateTimeOffset.UtcNow);

        Assert.False(branch.IsActive);
    }
}
