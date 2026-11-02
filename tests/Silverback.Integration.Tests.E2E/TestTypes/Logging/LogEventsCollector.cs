// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Integration.E2E.TestTypes.Logging;

public sealed class LogEventsCollector : ILoggerProvider, ILogger
{
    private readonly ConcurrentQueue<(EventId EventId, string Message)> _logEvents = new();

    public bool Contains(EventId eventId, string text) =>
        _logEvents.Any(logEvent => logEvent.EventId == eventId && logEvent.Message.Contains(text, StringComparison.Ordinal));

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _logEvents.Enqueue((eventId, formatter(state, exception)));

    public void Dispose()
    {
        // Nothing to dispose
    }
}
