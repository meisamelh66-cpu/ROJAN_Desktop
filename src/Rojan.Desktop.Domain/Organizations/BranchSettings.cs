namespace Rojan.Desktop.Domain.Organizations;

/// <summary>The full operating configuration for one <see cref="Branch"/> - business hours/working days, VAT, receipt template, appointment policy, and notification preferences, each its own sub-record per the phase's explicit "Branch Settings" requirement.</summary>
public sealed record BranchSettings(
    string BranchId,
    BusinessHours BusinessHours,
    IReadOnlyList<DayOfWeek> WorkingDays,
    decimal VatPercentage,
    ReceiptSettings ReceiptSettings,
    AppointmentRules AppointmentRules,
    NotificationSettings NotificationSettings);
