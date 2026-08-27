using Microsoft.Extensions.Logging;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.4.1 Production Hardening - minimal <see cref="ILogger{T}"/> test double that records every log call, so a test can assert a specific failure was actually logged (not just handled) without depending on any real logging provider/sink.</summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
