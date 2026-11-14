// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Tests.Integration.E2E.TestTypes.Logging;

public sealed class LogEventsCollector : ILoggerProvider, ILogger
{
    private readonly ConcurrentQueue<(EventId EventId, string Message)> _logEvents = new();

    public bool Contains(EventId eventId, string text) =>
        _logEvents.Any(logEvent => logEvent.EventId == eventId && logEvent.Message.Contains(text, StringComparison.Ordinal));

    // Whether a channel started stopping, because of a stop or, with Kafka, of a rollback or a revocation (the MQTT rollback doesn't write
    // this trace): it's written right before the reading of the channel is canceled
    public bool ContainsChannelStopping() =>
        Contains(IntegrationLogEvents.Tracing.EventId, "Stopping processing loop of channel");

    // Whether a stop is waiting for the consumer to stop: the trace is written after the reading of the channels was canceled
    public bool ContainsWaitingUntilConsumerStops() =>
        Contains(IntegrationLogEvents.Tracing.EventId, "Waiting until consumer stops...");

    // Whether the processing of a sequence (e.g. "BatchSequence batch-") was aborted, but not because of an error: the log is written
    // after the sequence was removed from its store and its processing was notified as completed
    public bool ContainsSequenceAborted(string sequenceTypeAndIdPrefix) =>
        Contains(IntegrationLogEvents.SequenceProcessingAborted.EventId, sequenceTypeAndIdPrefix);

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
