using Microsoft.Extensions.Logging;

namespace Itacr.Database.Migrator.Tests;

/// <summary>Keeps the messages of the log, so a test can check what the migrator reported.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) => _messages.Add(formatter(state, exception));
}
