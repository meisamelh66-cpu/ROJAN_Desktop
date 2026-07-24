using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Domain.Tests.Authentication;

public sealed class OrganizationRulesTests
{
    [Theory]
    [InlineData(OrganizationStatus.Active, OrganizationStatus.Suspended)]
    [InlineData(OrganizationStatus.Suspended, OrganizationStatus.Active)]
    public void IsValidTransition_AllowedTransition_ReturnsTrue(OrganizationStatus from, OrganizationStatus to)
    {
        Assert.True(OrganizationRules.IsValidTransition(from, to));
    }

    [Fact]
    public void Organization_ActiveStatus_IsActiveReturnsTrue()
    {
        var organization = new Organization("org-1", "Rojan Salon", OrganizationStatus.Active, DateTimeOffset.UtcNow);

        Assert.True(organization.IsActive);
    }

    [Fact]
    public void Organization_SuspendedStatus_IsActiveReturnsFalse()
    {
        var organization = new Organization("org-1", "Rojan Salon", OrganizationStatus.Suspended, DateTimeOffset.UtcNow);

        Assert.False(organization.IsActive);
    }
}
