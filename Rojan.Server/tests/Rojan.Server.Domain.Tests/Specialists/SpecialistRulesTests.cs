using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Domain.Tests.Specialists;

public sealed class SpecialistRulesTests
{
    [Theory]
    [InlineData("Priya Anand")]
    [InlineData("A")]
    public void IsValidName_NonEmptyName_ReturnsTrue(string fullName)
    {
        Assert.True(SpecialistRules.IsValidName(fullName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValidName_EmptyOrWhitespace_ReturnsFalse(string fullName)
    {
        Assert.False(SpecialistRules.IsValidName(fullName));
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("+1 555 020 1001")]
    public void IsValidPhone_NonEmptyPhone_ReturnsTrue(string phone)
    {
        Assert.True(SpecialistRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValidPhone_EmptyOrWhitespace_ReturnsFalse(string phone)
    {
        Assert.False(SpecialistRules.IsValidPhone(phone));
    }

    [Fact]
    public void IsValidEmail_Null_ReturnsTrue()
    {
        // Email is optional - null is always valid.
        Assert.True(SpecialistRules.IsValidEmail(null));
    }

    [Theory]
    [InlineData("specialist@rojan.example")]
    [InlineData("first.last@sub.rojan.example")]
    public void IsValidEmail_WellFormedAddress_ReturnsTrue(string email)
    {
        Assert.True(SpecialistRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@rojan.example")]
    [InlineData("specialist@")]
    [InlineData("specialist@@rojan.example")]
    public void IsValidEmail_MalformedAddress_ReturnsFalse(string email)
    {
        Assert.False(SpecialistRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData(SpecialistStatus.Active, SpecialistStatus.Inactive)]
    [InlineData(SpecialistStatus.Inactive, SpecialistStatus.Active)]
    public void IsValidTransition_AllowedTransition_ReturnsTrue(SpecialistStatus from, SpecialistStatus to)
    {
        Assert.True(SpecialistRules.IsValidTransition(from, to));
    }

    [Fact]
    public void Specialist_ActiveStatus_IsActiveReturnsTrue()
    {
        var specialist = new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.True(specialist.IsActive);
    }

    [Fact]
    public void Specialist_InactiveStatus_IsActiveReturnsFalse()
    {
        var specialist = new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Inactive, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.False(specialist.IsActive);
    }
}
