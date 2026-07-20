namespace Rojan.Desktop.Domain.Organizations;

/// <summary>A branch's customer-notification preferences.</summary>
public sealed record NotificationSettings(bool EmailEnabled, bool SmsEnabled, int ReminderHoursBeforeAppointment);
