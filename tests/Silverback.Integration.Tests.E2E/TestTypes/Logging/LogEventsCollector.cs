// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Tests.Integration.E2E.TestTypes.Logging;

public sealed partial class LogEventsCollector : ILoggerProvider, ILogger
{
    private readonly ConcurrentQueue<(EventId EventId, string Message, Exception? Exception)> _logEvents = new();

    public bool Contains(EventId eventId, string text) =>
        _logEvents.Any(logEvent => logEvent.EventId == eventId && logEvent.Message.Contains(text, StringComparison.Ordinal));

    public int Count(EventId eventId) => _logEvents.Count(logEvent => logEvent.EventId == eventId);

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

    // Whether adding a message to a sequence of the specified type was canceled (e.g. refused because the timeout of the sequence elapsed):
    // the trace is written right after the cancellation, before waiting for the sequence to be processed
    public bool ContainsSequenceAddCanceled(string sequenceType) =>
        Contains(IntegrationLogEvents.Tracing.EventId, $"skipping add to {sequenceType} ");

    // Whether a sequence of the specified type started completing (e.g. because its max wait time elapsed): the trace is written right
    // before its streams are completed
    public bool ContainsSequenceCompleting(string sequenceType) =>
        Contains(IntegrationLogEvents.Tracing.EventId, $"Completing {sequenceType} ");

    // Whether a fatal error was logged processing a consumed message (optionally only for the exceptions matching the predicate): the
    // exception reached the FatalExceptionLoggerConsumerBehavior, which disconnects the client
    public bool ContainsProcessingFatalError(Func<Exception?, bool>? exceptionPredicate = null) =>
        _logEvents.Any(logEvent =>
            logEvent.EventId == IntegrationLogEvents.ProcessingConsumedMessageFatalError.EventId &&
            (exceptionPredicate == null || exceptionPredicate(logEvent.Exception)));

    // The offsets stored by the Kafka consumers to be committed (the next offset to be consumed), in the order they were stored: the trace
    // is written right before the offset is stored
    public IReadOnlyList<long> GetStoredKafkaOffsets() =>
    [
        .. _logEvents
            .Where(logEvent => logEvent.EventId == IntegrationLogEvents.Tracing.EventId)
            .Select(logEvent => StoredOffsetRegex().Match(logEvent.Message))
            .Where(match => match.Success)
            .Select(match => long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
    ];

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
        _logEvents.Enqueue((eventId, formatter(state, exception), exception));

    public void Dispose()
    {
        // Nothing to dispose
    }

    [GeneratedRegex(@"^Storing offset .+@(\d+) \|", RegexOptions.CultureInvariant)]
    private static partial Regex StoredOffsetRegex();
}
