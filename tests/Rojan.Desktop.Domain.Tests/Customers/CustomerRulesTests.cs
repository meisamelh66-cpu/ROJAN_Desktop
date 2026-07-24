using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Domain.Tests.Customers;

public sealed class CustomerRulesTests
{
    [Theory]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Prospect, true)]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Active, true)]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Churned, true)]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Vip, false)]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Inactive, false)]
    [InlineData(CustomerStatus.Lead, CustomerStatus.Lead, false)]
    [InlineData(CustomerStatus.Prospect, CustomerStatus.Active, true)]
    [InlineData(CustomerStatus.Prospect, CustomerStatus.Churned, true)]
    [InlineData(CustomerStatus.Prospect, CustomerStatus.Lead, false)]
    [InlineData(CustomerStatus.Prospect, CustomerStatus.Vip, false)]
    [InlineData(CustomerStatus.Active, CustomerStatus.Vip, true)]
    [InlineData(CustomerStatus.Active, CustomerStatus.Inactive, true)]
    [InlineData(CustomerStatus.Active, CustomerStatus.Churned, true)]
    [InlineData(CustomerStatus.Active, CustomerStatus.Lead, false)]
    [InlineData(CustomerStatus.Active, CustomerStatus.Prospect, false)]
    [InlineData(CustomerStatus.Vip, CustomerStatus.Active, true)]
    [InlineData(CustomerStatus.Vip, CustomerStatus.Inactive, true)]
    [InlineData(CustomerStatus.Vip, CustomerStatus.Churned, true)]
    [InlineData(CustomerStatus.Vip, CustomerStatus.Lead, false)]
    [InlineData(CustomerStatus.Inactive, CustomerStatus.Active, true)]
    [InlineData(CustomerStatus.Inactive, CustomerStatus.Churned, true)]
    [InlineData(CustomerStatus.Inactive, CustomerStatus.Vip, false)]
    [InlineData(CustomerStatus.Inactive, CustomerStatus.Lead, false)]
    [InlineData(CustomerStatus.Churned, CustomerStatus.Lead, true)]
    [InlineData(CustomerStatus.Churned, CustomerStatus.Active, false)]
    [InlineData(CustomerStatus.Churned, CustomerStatus.Prospect, false)]
    [InlineData(CustomerStatus.Churned, CustomerStatus.Churned, false)]
    public void IsValidTransition_VariousPairs_MatchesExpectedLifecycle(CustomerStatus from, CustomerStatus to, bool expected)
    {
        Assert.Equal(expected, CustomerRules.IsValidTransition(from, to));
    }

    [Fact]
    public void IsValidTransition_EveryStatus_HasAtLeastOneWayOut()
    {
        // Unlike BookingRules, no CustomerStatus is fully terminal - a customer relationship is
        // ongoing, not a one-shot event.
        foreach (var status in Enum.GetValues<CustomerStatus>())
        {
            var hasAnyValidTransition = Enum.GetValues<CustomerStatus>()
                .Any(target => CustomerRules.IsValidTransition(status, target));

            Assert.True(hasAnyValidTransition, $"{status} should have at least one valid outgoing transition.");
        }
    }
}
