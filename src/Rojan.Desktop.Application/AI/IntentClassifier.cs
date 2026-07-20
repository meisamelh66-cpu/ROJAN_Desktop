namespace Rojan.Desktop.Application.AI;

public sealed class IntentClassifier : IIntentClassifier
{
    private static readonly IReadOnlyList<(InsightCategory Category, string[] Keywords)> KeywordMap =
    [
        (InsightCategory.Revenue, ["revenue", "sales", "income", "earning", "invoice"]),
        (InsightCategory.Customer, ["customer", "client", "retention", "churn"]),
        (InsightCategory.Appointment, ["appointment", "booking", "schedule", "calendar"]),
        (InsightCategory.Inventory, ["inventory", "stock", "product", "supply"]),
        (InsightCategory.Hr, ["staff", "employee", "team", "hr"]),
        (InsightCategory.Payroll, ["payroll", "salary", "pay"]),
        (InsightCategory.Attendance, ["attendance", "late", "absent", "present"]),
        (InsightCategory.Commission, ["commission", "specialist performance"]),
    ];

    public InsightCategory ClassifyIntent(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return InsightCategory.General;
        }

        foreach (var (category, keywords) in KeywordMap)
        {
            if (keywords.Any(keyword => userMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return category;
            }
        }

        return InsightCategory.General;
    }
}
