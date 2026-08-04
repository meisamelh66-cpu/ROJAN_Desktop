namespace Rojan.Desktop.Presentation.Threading;

/// <summary>
/// Schedules a one-shot delayed callback without a ViewModel taking a
/// direct dependency on <see cref="System.Windows.Threading.DispatcherTimer"/> -
/// <c>ArchitectureTests.ViewModelTestabilityTests</c> forbids exactly that
/// ("ViewModels must be testable without a running Dispatcher"). Same shape
/// as <c>Notifications.IToastDismissScheduler</c> (kept separate rather than
/// reused - that one's name and doc comment are toast-specific; this one is
/// for any ViewModel that needs a testable delay, starting with
/// <c>MobileOtpLoginViewModel</c>'s resend cooldown).
/// </summary>
public interface IDelayScheduler
{
    /// <summary>Invokes <paramref name="callback"/> once, after <paramref name="delay"/> elapses. Returns a handle that cancels the pending callback if disposed before it fires.</summary>
    public IDisposable Schedule(TimeSpan delay, Action callback);
}
