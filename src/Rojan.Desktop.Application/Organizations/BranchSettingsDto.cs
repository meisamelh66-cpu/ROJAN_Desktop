namespace Rojan.Desktop.Application.Organizations;

public sealed record BranchSettingsDto(
    string BranchId,
    BusinessHoursDto BusinessHours,
    IReadOnlyList<DayOfWeek> WorkingDays,
    decimal VatPercentage,
    ReceiptSettingsDto ReceiptSettings,
    AppointmentRulesDto AppointmentRules,
    NotificationSettingsDto NotificationSettings);
