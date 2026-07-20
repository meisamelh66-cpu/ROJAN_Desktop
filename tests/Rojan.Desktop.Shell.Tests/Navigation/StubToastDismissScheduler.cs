using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>No-op <see cref="IToastDismissScheduler"/> test double - never actually invokes the callback, since these navigation/branch-switcher tests never exercise toast auto-dismiss.</summary>
internal sealed class StubToastDismissScheduler : IToastDismissScheduler
{
    private sealed class NoOpHandle : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public IDisposable Schedule(TimeSpan delay, Action callback) => new NoOpHandle();
}
