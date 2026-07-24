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
        var branch = new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow);

        Assert.True(UserRules.IsValidBranchAssignment("org-1", branch));
    }

    [Fact]
    public void IsValidBranchAssignment_BranchBelongsToDifferentOrganization_ReturnsFalse()
    {
        // The core tenant-isolation rule: a branch from another organization must never be assignable.
        var branch = new Branch("branch-1", "org-2", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow);

        Assert.False(UserRules.IsValidBranchAssignment("org-1", branch));
    }

    private static User MakeUser(string organizationId, string id = "user-1") =>
        new(id, organizationId, BranchId: null, "owner@rojan.example", "hash", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow);

    [Fact]
    public void BelongsToOrganization_UserOrganizationIdMatches_ReturnsTrue()
    {
        var user = MakeUser("org-1");

        Assert.True(UserRules.BelongsToOrganization(user, "org-1"));
    }

    [Fact]
    public void BelongsToOrganization_UserOrganizationIdDoesNotMatch_ReturnsFalse()
    {
        var user = MakeUser("org-1");

        Assert.False(UserRules.BelongsToOrganization(user, "org-2"));
    }
}
