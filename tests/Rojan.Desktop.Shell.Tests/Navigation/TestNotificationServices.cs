using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// The real Notification stack (service/search/content-resolver) over
/// in-memory repository/preference stores - <see cref="MainWindowViewModel"/>'s
/// 3 Notification constructor parameters, factored out here since none
/// of these navigation/branch-switcher tests exercise Notification
/// Center behavior directly, the same reasoning
/// <c>TestHelpServices</c> already establishes for Phase 26's Help
/// dependencies.
/// </summary>
internal static class TestNotificationServices
{
    public static INotificationService CreateNotificationService() =>
        new NotificationService(new StubNotificationRepository(), new StubSilentModePreferenceStore());

    public static INotificationContentResolver ContentResolver { get; } = new NotificationContentResolver();

    public static INotificationSearchService SearchService { get; } = new NotificationSearchService();

    public static IToastDismissScheduler ToastDismissScheduler { get; } = new StubToastDismissScheduler();
}
