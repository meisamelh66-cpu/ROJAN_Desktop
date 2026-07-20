namespace Rojan.Desktop.Application.Organizations;

public sealed record NotificationSettingsDto(bool EmailEnabled, bool SmsEnabled, int ReminderHoursBeforeAppointment);
