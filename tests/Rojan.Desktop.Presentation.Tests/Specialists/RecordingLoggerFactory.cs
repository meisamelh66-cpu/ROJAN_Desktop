using Microsoft.Extensions.Logging;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>
/// Phase 8.43 - minimal <see cref="ILoggerFactory"/> test double for the profile-panel
/// parent→child plumbing. A parent page ViewModel holds an <see cref="ILoggerFactory"/>?
/// (not an <see cref="ILogger{T}"/> field - that would trip SYSLIB1020 alongside its own
/// <c>[LoggerMessage]</c>) and calls <c>CreateLogger&lt;TChild&gt;()</c> when it constructs a
/// child profile ViewModel. This factory records every log call routed through any logger it
/// hands out, tagged with the category name, so a test can assert the child's failure was
/// logged via the pass-through without depending on any real provider.
/// </summary>
public sealed class RecordingLoggerFactory : ILoggerFactory
{
    public List<(string Category, LogLevel Level, string Message)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CategoryLogger(categoryName, Entries);

    public void AddProvider(ILoggerProvider provider)
    {
        // No-op: this factory only records, it never fans out to real providers.
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    private sealed class CategoryLogger : ILogger
    {
        private readonly string _category;
        private readonly List<(string Category, LogLevel Level, string Message)> _sink;

        public CategoryLogger(string category, List<(string Category, LogLevel Level, string Message)> sink)
        {
            _category = category;
            _sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _sink.Add((_category, logLevel, formatter(state, exception)));
    }
}
