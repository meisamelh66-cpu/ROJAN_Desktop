using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.Tests.Notifications;

/// <summary>Exercises <see cref="NotificationContentResolver"/>'s key resolution, message-arg formatting, and category/group label mapping.</summary>
public sealed class NotificationContentResolverTests
{
    private readonly NotificationContentResolver _resolver = new();

    private static NotificationDto Notification(string titleKey, string messageKey, IReadOnlyList<string>? args = null, string category = "system", string? groupKey = null) =>
        new("n1", NotificationSeverity.Information, NotificationPriority.Normal, titleKey, messageKey, args ?? [], category, groupKey ?? category, DateTimeOffset.UtcNow, false);

    [Fact]
    public void Resolve_ResolvesTitleAndMessageKeysToLocalizedText()
    {
        var resolved = _resolver.Resolve(Notification(nameof(Strings.Notification_Demo_WelcomeTitle), nameof(Strings.Notification_Demo_WelcomeMessage)));

        Assert.NotEmpty(resolved.Title);
        Assert.NotEqual(nameof(Strings.Notification_Demo_WelcomeTitle), resolved.Title);
    }

    [Fact]
    public void Resolve_FormatsMessageArgsIntoTheTemplate()
    {
        var resolved = _resolver.Resolve(Notification(
            nameof(Strings.Notification_Demo_LowStockTitle),
            nameof(Strings.Notification_Demo_LowStockMessage),
            ["Aromatherapy Oil"]));

        Assert.Contains("Aromatherapy Oil", resolved.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_KnownCategory_ResolvesToLocalizedCategoryLabel()
    {
        var resolved = _resolver.Resolve(Notification("TitleKey", "MessageKey", category: "customers"));

        Assert.Equal(Strings.Notification_Category_Customers, resolved.CategoryLabel);
    }

    [Fact]
    public void Resolve_UnknownCategory_FallsBackToTheRawCategoryString()
    {
        var resolved = _resolver.Resolve(Notification("TitleKey", "MessageKey", category: "some-future-module"));

        Assert.Equal("some-future-module", resolved.CategoryLabel);
    }

    [Fact]
    public void Resolve_PreservesIdentityFields()
    {
        var resolved = _resolver.Resolve(Notification("TitleKey", "MessageKey"));

        Assert.Equal("n1", resolved.Id);
        Assert.Equal(NotificationSeverity.Information, resolved.Severity);
        Assert.Equal(NotificationPriority.Normal, resolved.Priority);
    }
}
