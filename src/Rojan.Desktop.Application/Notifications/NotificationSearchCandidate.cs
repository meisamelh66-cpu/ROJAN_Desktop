namespace Rojan.Desktop.Application.Notifications;

/// <summary>Already-resolved plain text for one notification, supplied by Presentation after resolving its <see cref="NotificationDto.TitleKey"/>/<see cref="NotificationDto.MessageKey"/> - keeps <see cref="NotificationSearchService"/>'s matching/scoring algorithm localization-agnostic and unit-testable, the same split <c>Application.Help.HelpSearchCandidate</c> established.</summary>
public sealed record NotificationSearchCandidate(string NotificationId, string Title, string Message);
