namespace Rojan.Desktop.Domain.Support;

/// <summary>Pure validation for Support Center submissions - no I/O, callable from Application before ever touching a repository.</summary>
public static class SupportRules
{
    /// <summary>Required fields for a "درخواست مشارکت در توسعه" (development-participation) submission - name, a way to reach the applicant, and what they're proposing. GitHub/LinkedIn/portfolio/resume stay optional (not every contributor has all four).</summary>
    public static IReadOnlyList<string> ValidateDevelopmentApplication(string firstName, string lastName, string mobile, string email, string collaborationArea)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add("LastName is required.");
        }

        if (string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(email))
        {
            errors.Add("At least one contact method (mobile or email) is required.");
        }

        if (string.IsNullOrWhiteSpace(collaborationArea))
        {
            errors.Add("CollaborationArea is required.");
        }

        return errors;
    }

    /// <summary>Required fields for any <see cref="SupportMessage"/>, regardless of <see cref="SupportMessageType"/> - a message needs actual content and a way to reply.</summary>
    public static IReadOnlyList<string> ValidateSupportMessage(string subject, string body, string senderEmail)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(subject))
        {
            errors.Add("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            errors.Add("Body is required.");
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            errors.Add("SenderEmail is required.");
        }

        return errors;
    }
}
