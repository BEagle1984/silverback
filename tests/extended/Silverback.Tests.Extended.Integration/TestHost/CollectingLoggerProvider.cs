// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Tests.Extended.Integration.TestHost;

// Collects the warnings and errors logged by the host under test, to assert on them and to write them to the test output
public sealed class CollectingLoggerProvider : ILoggerProvider, ILogger
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public IEnumerable<string> Errors => Entries.Where(entry => entry.Level >= LogLevel.Error).Select(entry => entry.ToString());

    public IEnumerable<LogEntry> OfEvents(params LogEvent[] logEvents) =>
        Entries.Where(entry => logEvents.Any(logEvent => logEvent.EventId == entry.EventId));

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
            Entries.Enqueue(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    public void Dispose()
    {
        // No resources to release
    }
}
