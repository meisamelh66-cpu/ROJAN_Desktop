using Microsoft.Extensions.Logging;

namespace Rojan.Desktop.Infrastructure.Observability;

/// <summary>
/// P1-5 - Desktop Observability Foundation, Phase 1 (<see cref="Api.HttpApiClient"/>
/// failure diagnostics only). Additive to the Generic Host's own default
/// Console/Debug providers (<c>Host.CreateDefaultBuilder()</c> already
/// wires those up; this is a second provider, not a replacement).
///
/// Persists to <c>%LocalAppData%\RojanDesktop\logs\rojandesktop-yyyy-MM-dd.log</c>,
/// the same "one concern, one file(-per-day) under RojanDesktop's
/// LocalAppData folder" convention every other persisted service in this
/// app already uses (see <c>ApiEnvironmentService</c>, <c>LocalSessionService</c>,
/// <c>SyncQueueService</c>, and every other <c>Local*</c> service's own doc
/// comment for the same pattern stated the same way).
///
/// Diagnostic-purpose only: this provider is deliberately content-agnostic
/// - it writes whatever a caller's <see cref="ILogger"/> call formats, the
/// same as any logging sink. It cannot itself decide what's safe to log;
/// that responsibility belongs entirely to the call site (see
/// <c>HttpApiClient</c>'s own doc comment on its log calls). Nothing in
/// this app reads this directory back - it is write-only from the
/// application's own perspective, so it can never become a second source
/// of business truth.
///
/// Both directory preparation/retention cleanup and every individual write
/// are wrapped so a failure (disk full, permissions, a locked file) is
/// silently dropped rather than thrown - logging must never turn into a
/// workflow failure of its own.
/// </summary>
public sealed class LocalFileLoggerProvider : ILoggerProvider
{
    private const string FileNamePrefix = "rojandesktop-";
    private const string FileNameSearchPattern = FileNamePrefix + "*.log";

    private readonly string _logDirectory;
    private readonly int _retentionDays;
    private readonly object _writeLock = new();

    public LocalFileLoggerProvider()
        : this(DefaultLogDirectory(), retentionDays: 14)
    {
    }

    /// <summary>Test-only seam - lets tests point at a temp directory and a short retention window instead of the real <see cref="DefaultLogDirectory"/>.</summary>
    internal LocalFileLoggerProvider(string logDirectory, int retentionDays)
    {
        _logDirectory = logDirectory;
        _retentionDays = retentionDays;
        TryPrepareDirectoryAndCleanUp();
    }

    public static string DefaultLogDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "logs");

    public ILogger CreateLogger(string categoryName) => new LocalFileLogger(categoryName, this);

    public void Dispose()
    {
    }

    internal void Write(string line)
    {
        try
        {
            lock (_writeLock)
            {
                Directory.CreateDirectory(_logDirectory);
                var path = Path.Combine(_logDirectory, $"{FileNamePrefix}{DateTimeOffset.UtcNow:yyyy-MM-dd}.log");
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort by design - see this class's own doc comment. A dropped log line is
            // never acceptable to surface as a failure of whatever real operation triggered it.
        }
    }

    private void TryPrepareDirectoryAndCleanUp()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var cutoffUtc = DateTime.UtcNow.AddDays(-_retentionDays);

            foreach (var file in Directory.EnumerateFiles(_logDirectory, FileNameSearchPattern))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoffUtc)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Same best-effort reasoning as Write - retention cleanup must never throw into startup.
        }
    }

    private sealed class LocalFileLogger(string categoryName, LocalFileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter is null)
            {
                return;
            }

            var message = formatter(state, exception);
            var line = exception is null
                ? $"{DateTimeOffset.UtcNow:O} [{logLevel}] {categoryName}: {message}"
                : $"{DateTimeOffset.UtcNow:O} [{logLevel}] {categoryName}: {message} | {exception.GetType().Name}: {exception.Message}";

            provider.Write(line);
        }
    }
}
