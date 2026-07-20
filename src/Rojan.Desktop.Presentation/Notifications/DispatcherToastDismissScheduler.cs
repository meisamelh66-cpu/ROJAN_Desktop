using System.Windows.Threading;

namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>Default <see cref="IToastDismissScheduler"/> - a one-shot <see cref="DispatcherTimer"/> per scheduled callback.</summary>
public sealed class DispatcherToastDismissScheduler : IToastDismissScheduler
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
