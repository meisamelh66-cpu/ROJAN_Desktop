using System.Windows.Threading;

namespace Rojan.Desktop.Presentation.Threading;

/// <summary>Default <see cref="IDelayScheduler"/> - a one-shot <see cref="DispatcherTimer"/> per scheduled callback, same shape as <c>Notifications.DispatcherToastDismissScheduler</c>.</summary>
public sealed class DispatcherDelayScheduler : IDelayScheduler
{
    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        var timer = new DispatcherTimer { Interval = delay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            callback();
        };
        timer.Start();
        return new TimerHandle(timer);
    }

    private sealed class TimerHandle : IDisposable
    {
        private readonly DispatcherTimer _timer;

        public TimerHandle(DispatcherTimer timer)
        {
            _timer = timer;
        }

        public void Dispose() => _timer.Stop();
    }
}
