using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>
/// Requirement 32.4's background runner - a <see cref="Timer"/> polling
/// <see cref="IScheduledJobService.GetDueJobsAsync"/> every
/// <see cref="CheckInterval"/> and running each due job via
/// <see cref="IScheduledJobService.RunDueJobAsync"/>. Started once from
/// Shell's composition root (<c>App.xaml.cs</c>'s <c>OnStartup</c>) and
/// stopped on app exit - deliberately a plain class with
/// <see cref="Start"/>/<see cref="Stop"/>, not a
/// <c>Microsoft.Extensions.Hosting.IHostedService</c>, since this
/// app's Generic Host is used for DI/config composition only and is never
/// itself <c>Run()</c> as a service host (the WPF <c>Application</c>
/// lifecycle is what actually drives this app - see
/// <c>docs/architecture/01-desktop-shell.md</c>).
/// </summary>
public sealed class WorkflowSchedulerService : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IScheduledJobService _scheduledJobService;
    private Timer? _timer;
    private int _isTickRunning;

    public WorkflowSchedulerService(IScheduledJobService scheduledJobService)
    {
        _scheduledJobService = scheduledJobService;
    }

    public void Start()
    {
        _timer ??= new Timer(_ => _ = TickAsync(), state: null, CheckInterval, CheckInterval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();

    /// <summary>
    /// Guarded by <see cref="_isTickRunning"/> against overlapping ticks if
    /// a run takes longer than <see cref="CheckInterval"/>. Requirement
    /// 32.11's Error Recovery/dead-letter-ready architecture: one job's
    /// failure never stops the scheduler from checking the rest or from
    /// ticking again next interval - the failed run itself already stayed
    /// queryable in execution history via <c>WorkflowExecutionEngine</c>,
    /// so swallowing the exception here doesn't lose it.
    /// </summary>
    private async Task TickAsync()
    {
        if (Interlocked.Exchange(ref _isTickRunning, 1) == 1)
        {
            return;
        }

        try
        {
            var dueJobs = await _scheduledJobService.GetDueJobsAsync().ConfigureAwait(false);
            foreach (var job in dueJobs)
            {
                try
                {
                    await _scheduledJobService.RunDueJobAsync(job.Id).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Swallowed deliberately - see this method's own doc comment.
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Defensive: the scheduler's background timer must never take down the app.
        }
        finally
        {
            Interlocked.Exchange(ref _isTickRunning, 0);
        }
    }
}
