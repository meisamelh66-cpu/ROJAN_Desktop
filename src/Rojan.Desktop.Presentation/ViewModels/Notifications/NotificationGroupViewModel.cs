using System.Collections.ObjectModel;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>Phase 27's Grouping requirement - one labeled section of the Notification Center's list (e.g. "Customers (3)"), built fresh on every <c>NotificationCenterViewModel</c> refresh.</summary>
public sealed class NotificationGroupViewModel
{
    public NotificationGroupViewModel(string groupLabel, IReadOnlyList<NotificationRowViewModel> items)
    {
        GroupLabel = groupLabel;
        Items = new ObservableCollection<NotificationRowViewModel>(items);
    }

    public string GroupLabel { get; }

    public ObservableCollection<NotificationRowViewModel> Items { get; }

    public int Count => Items.Count;
}
