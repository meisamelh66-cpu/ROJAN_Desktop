using Rojan.Desktop.Presentation.Threading;

namespace Rojan.Desktop.Presentation.Tests.Security;

/// <summary>Controllable <see cref="IDelayScheduler"/> test double - captures the scheduled callback so a test can invoke it manually instead of waiting on a real timer. Same shape as <c>Notifications.StubToastDismissScheduler</c>.</summary>
internal sealed class StubDelayScheduler : IDelayScheduler
{
    public List<Action> ScheduledCallbacks { get; } = [];

    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        ScheduledCallbacks.Add(callback);
        return new NoOpHandle();
    }

    /// <summary>Invokes every callback scheduled so far, e.g. to simulate a resend cooldown elapsing.</summary>
    public void FireAll()
    {
        foreach (var callback in ScheduledCallbacks.ToArray())
        {
            callback();
        }
    }

    private sealed class NoOpHandle : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
