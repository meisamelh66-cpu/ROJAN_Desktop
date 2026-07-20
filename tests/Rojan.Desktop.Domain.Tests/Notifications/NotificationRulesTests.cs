using Rojan.Desktop.Domain.Notifications;

namespace Rojan.Desktop.Domain.Tests.Notifications;

/// <summary>Exercises <see cref="NotificationRules"/>'s pure filtering/grouping/priority-ranking/Silent-Mode logic.</summary>
public sealed class NotificationRulesTests
{
    private static AppNotification Notification(
        NotificationSeverity severity = NotificationSeverity.Information,
        NotificationPriority priority = NotificationPriority.Normal,
        bool isRead = false,
        string category = "system",
        string? groupKey = null,
        bool isSilent = false) =>
        new("n1", severity, priority, "TitleKey", "MessageKey", [], category, groupKey, DateTimeOffset.UtcNow, isRead, isSilent);

    [Fact]
    public void Matches_NoFilterAxesSet_AlwaysMatches()
    {
        var notification = Notification();

        Assert.True(NotificationRules.Matches(notification, new NotificationFilter()));
    }

    [Fact]
    public void Matches_SeverityFilterSetToDifferentSeverity_DoesNotMatch()
    {
        var notification = Notification(severity: NotificationSeverity.Error);

        Assert.False(NotificationRules.Matches(notification, new NotificationFilter(Severity: NotificationSeverity.Warning)));
    }

    [Fact]
    public void Matches_SeverityFilterSetToSameSeverity_Matches()
    {
        var notification = Notification(severity: NotificationSeverity.Error);

        Assert.True(NotificationRules.Matches(notification, new NotificationFilter(Severity: NotificationSeverity.Error)));
    }

    [Fact]
    public void Matches_PriorityFilterSetToDifferentPriority_DoesNotMatch()
    {
        var notification = Notification(priority: NotificationPriority.Critical);

        Assert.False(NotificationRules.Matches(notification, new NotificationFilter(Priority: NotificationPriority.Low)));
    }

    [Fact]
    public void Matches_IsReadFilterMismatch_DoesNotMatch()
    {
        var notification = Notification(isRead: false);

        Assert.False(NotificationRules.Matches(notification, new NotificationFilter(IsRead: true)));
    }

    [Fact]
    public void Matches_CategoryFilterCaseInsensitive_Matches()
    {
        var notification = Notification(category: "Customers");

        Assert.True(NotificationRules.Matches(notification, new NotificationFilter(Category: "customers")));
    }

    [Fact]
    public void GroupKeyFor_ExplicitGroupKeySet_ReturnsExplicitGroupKey()
    {
        var notification = Notification(category: "bookings", groupKey: "booking-status-change");

        Assert.Equal("booking-status-change", NotificationRules.GroupKeyFor(notification));
    }

    [Fact]
    public void GroupKeyFor_NoExplicitGroupKey_FallsBackToCategory()
    {
        var notification = Notification(category: "inventory", groupKey: null);

        Assert.Equal("inventory", NotificationRules.GroupKeyFor(notification));
    }

    [Theory]
    [InlineData(NotificationPriority.Critical, 3)]
    [InlineData(NotificationPriority.High, 2)]
    [InlineData(NotificationPriority.Normal, 1)]
    [InlineData(NotificationPriority.Low, 0)]
    public void PriorityRank_OrdersHighestFirst(NotificationPriority priority, int expectedRank)
    {
        Assert.Equal(expectedRank, NotificationRules.PriorityRank(priority));
    }

    [Fact]
    public void ShouldShowToast_SilentModeDisabled_AlwaysShowsToast()
    {
        var notification = Notification(priority: NotificationPriority.Low);

        Assert.True(NotificationRules.ShouldShowToast(notification, isSilentModeEnabled: false));
    }

    [Fact]
    public void ShouldShowToast_SilentModeEnabledNonCriticalPriority_SuppressesToast()
    {
        var notification = Notification(priority: NotificationPriority.High);

        Assert.False(NotificationRules.ShouldShowToast(notification, isSilentModeEnabled: true));
    }

    [Fact]
    public void ShouldShowToast_SilentModeEnabledCriticalPriority_StillShowsToast()
    {
        var notification = Notification(priority: NotificationPriority.Critical);

        Assert.True(NotificationRules.ShouldShowToast(notification, isSilentModeEnabled: true));
    }

    [Fact]
    public void ShouldShowToast_PerNotificationIsSilentOverride_SuppressesToastEvenWhenCriticalAndSilentModeOff()
    {
        var notification = Notification(priority: NotificationPriority.Critical, isSilent: true);

        Assert.False(NotificationRules.ShouldShowToast(notification, isSilentModeEnabled: false));
    }
}
