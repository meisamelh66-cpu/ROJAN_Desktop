using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Domain.Tests.HR;

public sealed class EmployeeStatusRulesTests
{
    [Theory]
    [InlineData(EmployeeStatus.Inactive)]
    [InlineData(EmployeeStatus.Suspended)]
    [InlineData(EmployeeStatus.OnLeave)]
    public void CanActivate_NotAlreadyActive_ReturnsTrue(EmployeeStatus current)
    {
        Assert.True(EmployeeStatusRules.CanActivate(current));
    }

    [Fact]
    public void CanActivate_AlreadyActive_ReturnsFalse()
    {
        Assert.False(EmployeeStatusRules.CanActivate(EmployeeStatus.Active));
    }

    [Fact]
    public void CanDeactivate_AlreadyInactive_ReturnsFalse()
    {
        Assert.False(EmployeeStatusRules.CanDeactivate(EmployeeStatus.Inactive));
    }

    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.OnLeave)]
    public void CanSuspend_ActiveOrOnLeave_ReturnsTrue(EmployeeStatus current)
    {
        Assert.True(EmployeeStatusRules.CanSuspend(current));
    }

    [Theory]
    [InlineData(EmployeeStatus.Inactive)]
    [InlineData(EmployeeStatus.Suspended)]
    public void CanSuspend_InactiveOrAlreadySuspended_ReturnsFalse(EmployeeStatus current)
    {
        Assert.False(EmployeeStatusRules.CanSuspend(current));
    }
}
