using Microsoft.Extensions.Logging;

namespace unilib.Tests;

// Keeps every log entry in memory so a test can check what was written.
public sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));

    public string AllText => string.Join("\n", Entries.Select(e => e.Message));
}
