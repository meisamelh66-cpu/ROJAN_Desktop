using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>
/// Drives DashboardPage. Phase 06A is layout/architecture only - every
/// collection below is static placeholder data held directly in the
/// ViewModel, not loaded from any service/repository. No Application or
/// Infrastructure dependency, no persistence.
/// </summary>
public sealed class DashboardPageViewModel : ViewModelBase
{
    public DashboardPageViewModel()
    {
        KpiCards = new ObservableCollection<KpiCardItem>
        {
            new("Total Bookings", "128"),
            new("Active Clients", "42"),
            new("Revenue (MTD)", "$12,400"),
            new("Pending Tasks", "7"),
        };

        QuickActions = new ObservableCollection<QuickActionItem>
        {
            new("New Booking"),
            new("Add Client"),
            new("Create Task"),
            new("View Reports"),
        };

        RecentActivity = new ObservableCollection<ActivityItem>
        {
            new("New booking created", "2 min ago"),
            new("Client profile updated", "1 hour ago"),
            new("Payment received", "3 hours ago"),
            new("Task completed", "Yesterday"),
        };

        QuickActionCommand = new RelayCommand(_ => { });
    }

    public ObservableCollection<KpiCardItem> KpiCards { get; }

    public ObservableCollection<QuickActionItem> QuickActions { get; }

    public ObservableCollection<ActivityItem> RecentActivity { get; }

    /// <summary>Bound by every Quick Action button. Intentionally a no-op - Phase 06A is layout only, no business logic.</summary>
    public ICommand QuickActionCommand { get; }
}
