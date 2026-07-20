namespace Rojan.Desktop.Domain.AI;

/// <summary>Which business area an <see cref="AIInsight"/> or <see cref="AIRecommendation"/> is about - one category per module the InsightEngine reads from.</summary>
public enum InsightCategory
{
    Revenue,
    Customer,
    Appointment,
    Inventory,
    Hr,
    Payroll,
    Attendance,
    Commission,
    General,
}
