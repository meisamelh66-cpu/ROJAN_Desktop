namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>
/// Phase 27: schedules a toast's auto-dismiss without <c>ToastHostViewModel</c>
/// (or any type in the <c>ViewModels</c> namespace) taking a direct
/// dependency on <see cref="System.Windows.Threading.DispatcherTimer"/> -
/// <c>ArchitectureTests.ViewModelTestabilityTests</c> forbids exactly
/// that ("ViewModels must be testable without a running Dispatcher").
/// The default implementation (<c>DispatcherToastDismissScheduler</c>)
/// lives outside the <c>ViewModels</c> namespace specifically so it can
/// use the real WPF dispatcher while the ViewModel itself stays
/// dispatcher-free and unit-testable with a fake scheduler.
/// </summary>
public interface IToastDismissScheduler
{
    /// <summary>Invokes <paramref name="callback"/> once, after <paramref name="delay"/> elapses. Returns a handle that cancels the pending callback if disposed before it fires.</summary>
    public IDisposable Schedule(TimeSpan delay, Action callback);
}
