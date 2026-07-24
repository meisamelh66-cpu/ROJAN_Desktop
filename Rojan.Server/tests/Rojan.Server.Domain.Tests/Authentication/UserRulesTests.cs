using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Domain.Tests.Authentication;

public sealed class UserRulesTests
{
    [Theory]
    [InlineData("owner@rojan.example")]
    [InlineData("first.last@sub.rojan.example")]
    public void IsValidEmail_WellFormedAddress_ReturnsTrue(string email)
    {
        Assert.True(UserRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("@rojan.example")]
    [InlineData("owner@")]
    [InlineData("owner@@rojan.example")]
    [InlineData("owner-at-rojan-example")]
    public void IsValidEmail_MalformedAddress_ReturnsFalse(string email)
    {
        Assert.False(UserRules.IsValidEmail(email));
    }

    [Fact]
    public void IsValidBranchAssignment_NoBranch_ReturnsTrue()
    {
        Assert.True(UserRules.IsValidBranchAssignment("org-1", branch: null));
    }

    [Fact]
    public void IsValidBranchAssignment_BranchBelongsToSameOrganization_ReturnsTrue()
    {
        var branch = new Branch("branch-1", "org-1", "Downtown", DateTimeOffset.UtcNow);

        Assert.True(UserRules.IsValidBranchAssignment("org-1", branch));
    }

    [Fact]
    public void IsValidBranchAssignment_BranchBelongsToDifferentOrganization_ReturnsFalse()
    {
        // The core tenant-isolation rule: a branch from another organization must never be assignable.
        var branch = new Branch("branch-1", "org-2", "Downtown", DateTimeOffset.UtcNow);

        Assert.False(UserRules.IsValidBranchAssignment("org-1", branch));
    }
}
