using Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Domain.Tests.Support;

public sealed class SupportRulesTests
{
    [Fact]
    public void ValidateDevelopmentApplication_AllFieldsProvided_ReturnsNoErrors()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("Sara", "Ahmadi", "0912-000-0000", "sara@example.com", "Backend");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDevelopmentApplication_MissingFirstName_ReturnsError()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("", "Ahmadi", "0912-000-0000", "sara@example.com", "Backend");

        Assert.Contains(errors, error => error.Contains("FirstName", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDevelopmentApplication_MissingLastName_ReturnsError()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("Sara", "", "0912-000-0000", "sara@example.com", "Backend");

        Assert.Contains(errors, error => error.Contains("LastName", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDevelopmentApplication_NoContactMethod_ReturnsError()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("Sara", "Ahmadi", "", "", "Backend");

        Assert.Contains(errors, error => error.Contains("contact method", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateDevelopmentApplication_OnlyMobileProvided_ReturnsNoContactError()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("Sara", "Ahmadi", "0912-000-0000", "", "Backend");

        Assert.DoesNotContain(errors, error => error.Contains("contact method", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateDevelopmentApplication_MissingCollaborationArea_ReturnsError()
    {
        var errors = SupportRules.ValidateDevelopmentApplication("Sara", "Ahmadi", "0912-000-0000", "sara@example.com", "");

        Assert.Contains(errors, error => error.Contains("CollaborationArea", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateSupportMessage_AllFieldsProvided_ReturnsNoErrors()
    {
        var errors = SupportRules.ValidateSupportMessage("Subject", "Body", "sara@example.com");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("", "Body", "sara@example.com")]
    [InlineData("Subject", "", "sara@example.com")]
    [InlineData("Subject", "Body", "")]
    public void ValidateSupportMessage_MissingRequiredField_ReturnsError(string subject, string body, string senderEmail)
    {
        var errors = SupportRules.ValidateSupportMessage(subject, body, senderEmail);

        Assert.NotEmpty(errors);
    }
}
