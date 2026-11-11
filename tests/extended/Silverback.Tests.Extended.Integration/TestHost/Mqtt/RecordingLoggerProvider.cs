// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

/// <summary>
///     Records the information, warning and error logs, to be written to the test output.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider, ILogger
{
    private readonly ConcurrentQueue<string> _entries = new();

    public ConcurrentQueue<string> Errors { get; } = new();

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        string entry = $"{DateTime.Now:HH:mm:ss.fff} {logLevel} {eventId.Id}: {formatter(state, exception)}";

        if (exception != null)
            entry += $" | {exception.GetType().Name}: {exception.Message}";

        _entries.Enqueue(entry);

        if (logLevel >= LogLevel.Error)
            Errors.Enqueue(entry);
    }

    public void WriteTo(ITestOutputHelper output)
    {
        foreach (string entry in _entries)
        {
            output.WriteLine($"Log: {entry}");
        }
    }

    public void Dispose()
    {
        // No resources to release
    }
}
