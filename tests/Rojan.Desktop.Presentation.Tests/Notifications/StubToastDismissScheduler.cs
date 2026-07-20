using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.Tests.Notifications;

/// <summary>Controllable <see cref="IToastDismissScheduler"/> test double - captures the scheduled callback so a test can invoke it manually instead of waiting on a real timer.</summary>
internal sealed class StubToastDismissScheduler : IToastDismissScheduler
{
    public List<Action> ScheduledCallbacks { get; } = [];

    private sealed class NoOpHandle : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        ScheduledCallbacks.Add(callback);
        return new NoOpHandle();
    }
}
