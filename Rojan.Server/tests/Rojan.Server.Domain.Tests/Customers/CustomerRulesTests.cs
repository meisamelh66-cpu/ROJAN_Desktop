using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Domain.Tests.Customers;

public sealed class CustomerRulesTests
{
    [Theory]
    [InlineData("Noah Bennett")]
    [InlineData("A")]
    public void IsValidName_NonEmptyName_ReturnsTrue(string name)
    {
        Assert.True(CustomerRules.IsValidName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValidName_EmptyOrWhitespace_ReturnsFalse(string name)
    {
        Assert.False(CustomerRules.IsValidName(name));
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("+1 555 020 1001")]
    public void IsValidPhone_NonEmptyPhone_ReturnsTrue(string phone)
    {
        Assert.True(CustomerRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValidPhone_EmptyOrWhitespace_ReturnsFalse(string phone)
    {
        Assert.False(CustomerRules.IsValidPhone(phone));
    }

    [Fact]
    public void IsValidEmail_Null_ReturnsTrue()
    {
        // Email is optional - null is always valid.
        Assert.True(CustomerRules.IsValidEmail(null));
    }

    [Theory]
    [InlineData("customer@rojan.example")]
    [InlineData("first.last@sub.rojan.example")]
    public void IsValidEmail_WellFormedAddress_ReturnsTrue(string email)
    {
        Assert.True(CustomerRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@rojan.example")]
    [InlineData("customer@")]
    [InlineData("customer@@rojan.example")]
    public void IsValidEmail_MalformedAddress_ReturnsFalse(string email)
    {
        Assert.False(CustomerRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData(CustomerStatus.Active, CustomerStatus.Inactive)]
    [InlineData(CustomerStatus.Inactive, CustomerStatus.Active)]
    public void IsValidTransition_AllowedTransition_ReturnsTrue(CustomerStatus from, CustomerStatus to)
    {
        Assert.True(CustomerRules.IsValidTransition(from, to));
    }

    [Fact]
    public void Customer_ActiveStatus_IsActiveReturnsTrue()
    {
        var customer = new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Customer_InactiveStatus_IsActiveReturnsFalse()
    {
        var customer = new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Inactive, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.False(customer.IsActive);
    }
}
