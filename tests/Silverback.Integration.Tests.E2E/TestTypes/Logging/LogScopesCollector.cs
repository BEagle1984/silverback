// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Tests.Integration.E2E.TestTypes.Logging;

// Collects the scope values (e.g. the activity tags added via the ActivityTrackingOptions) that are available when the logs of the
// specified event are written, as a logging provider supporting the scopes does
public sealed class LogScopesCollector : ILoggerProvider, ISupportExternalScope, ILogger
{
    private readonly EventId _eventId;

    private readonly ConcurrentQueue<IReadOnlyDictionary<string, object?>> _scopes = new();

    private readonly ConcurrentQueue<string> _messages = new();

    private IExternalScopeProvider? _scopeProvider;

    public LogScopesCollector(LogEvent logEvent)
    {
        _eventId = logEvent.EventId;
    }

    public static Action<LoggerFactoryOptions> TrackActivity => options => options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.Tags;

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Scopes => [.. _scopes];

    public IReadOnlyList<string> Messages => [.. _messages];

    public ILogger CreateLogger(string categoryName) => this;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        _scopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (eventId != _eventId)
            return;

        Dictionary<string, object?> values = [];

        _scopeProvider?.ForEachScope(
            static (scope, scopeValues) =>
            {
                if (scope is not IEnumerable<KeyValuePair<string, object?>> keyValuePairs)
                    return;

                foreach (KeyValuePair<string, object?> keyValuePair in keyValuePairs.ToList())
                {
                    scopeValues[keyValuePair.Key] = keyValuePair.Value;
                }
            },
            values);

        _scopes.Enqueue(values);
        _messages.Enqueue(formatter(state, exception));
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
