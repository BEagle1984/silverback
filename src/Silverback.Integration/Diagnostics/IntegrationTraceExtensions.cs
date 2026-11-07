// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics;

// The traces are diagnostics, not a public contract: they share the IntegrationLogEvents.Tracing event id and, unlike the
// *LoggerExtensions classes, generate no public LogEvent
internal static partial class IntegrationTraceExtensions
{
    public static void LogStoppingAllConsumers(this ISilverbackLogger logger) =>
        StoppingAllConsumers(logger.InnerLogger);

    public static void LogAllConsumersStopped(this ISilverbackLogger logger) =>
        AllConsumersStopped(logger.InnerLogger);

    public static void LogDisconnectingAllClients(this ISilverbackLogger logger) =>
        DisconnectingAllClients(logger.InnerLogger);

    public static void LogAllClientsDisconnected(this ISilverbackLogger logger) =>
        AllClientsDisconnected(logger.InnerLogger);

    public static void LogConsumerStarting(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerStarting(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerStarted(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerStarted(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerStopping(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerStopping(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerStopped(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerStopped(logger.InnerLogger, consumer.DisplayName);

    public static void LogWaitingUntilConsumerStops(this ISilverbackLogger logger, IConsumer consumer) =>
        WaitingUntilConsumerStops(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerStopIgnored(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerStopIgnored(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerReconnectTriggered(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerReconnectTriggered(logger.InnerLogger, consumer.DisplayName);

    public static void LogConsumerReconnectIgnored(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerReconnectIgnored(logger.InnerLogger, consumer.DisplayName);

    public static void LogAllChannelsDisposed(this ISilverbackLogger logger, IConsumer consumer) =>
        AllChannelsDisposed(logger.InnerLogger, consumer.DisplayName);

    public static void LogChannelReading(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelReading(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopStarting(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopStarting(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopStopping(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopStopping(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopCanceled(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopCanceled(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopClosed(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopClosed(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopExited(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopExited(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopStartupDeferred(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopStartupDeferred(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopStartupIgnored(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopStartupIgnored(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogChannelProcessingLoopStartupSkipped(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        ChannelProcessingLoopStartupSkipped(logger.InnerLogger, channel.Id, consumer.DisplayName);

    public static void LogConsumerTransactionCommitting(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionCommitting(logger.InnerLogger, envelope.Endpoint.DisplayName, envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogConsumerTransactionCommitted(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionCommitted(logger.InnerLogger, envelope.Endpoint.DisplayName, envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogConsumerTransactionAlreadyCommitted(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionAlreadyCommitted(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogConsumerTransactionAborting(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception? exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionAborting(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogConsumerTransactionAborted(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionAborted(logger.InnerLogger, envelope.Endpoint.DisplayName, envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogConsumerTransactionAlreadyAborted(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ConsumerTransactionAlreadyAborted(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogProcessingCanceledByConsumerStop(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ProcessingCanceledByConsumerStop(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogAllErrorPoliciesApplied(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        AllErrorPoliciesApplied(logger.InnerLogger, envelope.Endpoint.DisplayName, envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogErrorPolicySkippedMaxFailedAttempts(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string policyType,
        int failedAttempts,
        int? maxFailedAttempts)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ErrorPolicySkippedMaxFailedAttempts(
            logger.InnerLogger,
            policyType,
            failedAttempts,
            maxFailedAttempts,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogErrorPolicySkippedExceptionNotIncluded(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string policyType,
        string exceptionType)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ErrorPolicySkippedExceptionNotIncluded(
            logger.InnerLogger,
            policyType,
            exceptionType,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogErrorPolicySkippedExceptionExcluded(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string policyType,
        string exceptionType)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ErrorPolicySkippedExceptionExcluded(
            logger.InnerLogger,
            policyType,
            exceptionType,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogErrorPolicySkippedApplyRule(this ISilverbackLogger logger, IRawInboundEnvelope envelope, string policyType)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        ErrorPolicySkippedApplyRule(
            logger.InnerLogger,
            policyType,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogRetryDelayWaiting(this ISilverbackLogger logger, IRawInboundEnvelope envelope, TimeSpan delay)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        RetryDelayWaiting(
            logger.InnerLogger,
            delay.TotalMilliseconds,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequencePublishing(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequencePublishing(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequencePublished(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        ISequence sequence,
        int processingTaskId)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequencePublished(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            processingTaskId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceNoSubscribers(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceNoSubscribers(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceSubscribersCompleted(
        this ISilverbackLogger logger,
        ISequence sequence,
        IReadOnlyCollection<Task> processingTasks)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceSubscribersCompleted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.IsComplete,
            processingTasks.Count(task => task.IsFaulted),
            processingTasks.Count);
    }

    public static void LogSequenceProcessingCompleted(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceProcessingCompleted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceProcessingCompletedNotOuterStart(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceProcessingCompletedNotOuterStart(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceStillPending(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        ISequence sequence,
        Task? processingTask)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceStillPending(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            processingTask?.Status,
            processingTask?.Id,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceEndedAwaitingProcessingTask(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceEndedAwaitingProcessingTask(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceEndedProcessingTaskCompleted(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceEndedProcessingTaskCompleted(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceProcessingTaskAwaiting(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceProcessingTaskAwaiting(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceProcessingTaskAwaited(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceProcessingTaskAwaited(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceProcessingAwaitingBeforeRethrow(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        SequenceProcessingAwaitingBeforeRethrow(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogSequenceAddCanceled(this ISilverbackLogger logger, ISequence sequence, Exception exception) =>
        SequenceAddCanceled(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, exception);

    public static void LogSequenceAddFailed(this ISilverbackLogger logger, ISequence sequence, Exception exception) =>
        SequenceAddFailed(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, exception);

    public static void LogSequenceCompleting(this ISilverbackLogger logger, ISequence sequence, int sequenceLength) =>
        SequenceCompleting(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, sequenceLength);

    public static void LogSequenceCompletingWithTotalLength(this ISilverbackLogger logger, ISequence sequence, int? sequenceLength) =>
        SequenceCompletingWithTotalLength(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, sequenceLength);

    public static void LogSequenceTimeoutElapsed(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceTimeoutElapsed(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceAborting(
        this ISilverbackLogger logger,
        ISequence sequence,
        SequenceAbortReason abortReason,
        Exception? exception) =>
        SequenceAborting(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, abortReason, exception);

    public static void LogSequenceAbortAwaitingAddSemaphore(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceAbortAwaitingAddSemaphore(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceDisposing(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceDisposing(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceDisposed(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceDisposed(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceDisposeAwaitingAbort(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceDisposeAwaitingAbort(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceDisposeAwaitingAddSemaphore(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceDisposeAwaitingAddSemaphore(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSequenceDisposeAwaitingCompleteSemaphore(this ISilverbackLogger logger, ISequence sequence) =>
        SequenceDisposeAwaitingCompleteSemaphore(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId);

    public static void LogSemaphoreWaiting(this ISilverbackLogger logger) =>
        SemaphoreWaiting(logger.InnerLogger);

    public static void LogSemaphoreAcquired(this ISilverbackLogger logger) =>
        SemaphoreAcquired(logger.InnerLogger);

    public static void LogSemaphoreWaitSkippedSequenceDisposed(this ISilverbackLogger logger) =>
        SemaphoreWaitSkippedSequenceDisposed(logger.InnerLogger);

    public static void LogSemaphoreWaitInterruptedSequenceDisposed(this ISilverbackLogger logger) =>
        SemaphoreWaitInterruptedSequenceDisposed(logger.InnerLogger);

    public static void LogSemaphoreObjectDisposed(this ISilverbackLogger logger) =>
        SemaphoreObjectDisposed(logger.InnerLogger);

    public static void LogSequenceAddedToStore(this ISilverbackLogger logger, ISequence sequence, Guid sequenceStoreId) =>
        SequenceAddedToStore(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, sequenceStoreId);

    public static void LogSequenceRemovedFromStore(this ISilverbackLogger logger, string sequenceId, Guid sequenceStoreId) =>
        SequenceRemovedFromStore(logger.InnerLogger, sequenceId, sequenceStoreId);

    public static void LogSequenceStoreDisposing(this ISilverbackLogger logger, Guid sequenceStoreId) =>
        SequenceStoreDisposing(logger.InnerLogger, sequenceStoreId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Stopping all consumers",
        EventName = "Silverback.Integration_Tracing_StoppingAllConsumers")]
    private static partial void StoppingAllConsumers(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "All consumers stopped",
        EventName = "Silverback.Integration_Tracing_AllConsumersStopped")]
    private static partial void AllConsumersStopped(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Disconnecting all clients",
        EventName = "Silverback.Integration_Tracing_DisconnectingAllClients")]
    private static partial void DisconnectingAllClients(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "All clients disconnected",
        EventName = "Silverback.Integration_Tracing_AllClientsDisconnected")]
    private static partial void AllClientsDisconnected(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Starting consumer... | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerStarting")]
    private static partial void ConsumerStarting(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Consumer started. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerStarted")]
    private static partial void ConsumerStarted(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Stopping consumer... | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerStopping")]
    private static partial void ConsumerStopping(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Consumer stopped. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerStopped")]
    private static partial void ConsumerStopped(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Waiting until consumer stops... | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_WaitingUntilConsumerStops")]
    private static partial void WaitingUntilConsumerStops(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Ignoring the stop request: the processing was already canceled. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerStopIgnored")]
    private static partial void ConsumerStopIgnored(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Triggering reconnect. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerReconnectTriggered")]
    private static partial void ConsumerReconnectTriggered(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Ignoring the reconnect request: the consumer is stopped or being stopped. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ConsumerReconnectIgnored")]
    private static partial void ConsumerReconnectIgnored(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "All channels disposed | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_AllChannelsDisposed")]
    private static partial void AllChannelsDisposed(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Reading channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelReading")]
    private static partial void ChannelReading(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Starting processing loop of channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopStarting")]
    private static partial void ChannelProcessingLoopStarting(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Stopping processing loop of channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopStopping")]
    private static partial void ChannelProcessingLoopStopping(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Exiting processing loop of channel {Channel} (operation canceled) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopCanceled")]
    private static partial void ChannelProcessingLoopCanceled(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Exiting processing loop of channel {Channel} (channel closed) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopClosed")]
    private static partial void ChannelProcessingLoopClosed(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Exited processing loop of channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopExited")]
    private static partial void ChannelProcessingLoopExited(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Deferring processing loop startup of channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopStartupDeferred")]
    private static partial void ChannelProcessingLoopStartupDeferred(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Ignoring processing loop startup of channel {Channel} (still reading) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopStartupIgnored")]
    private static partial void ChannelProcessingLoopStartupIgnored(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Skipping deferred processing loop startup of channel {Channel} (stopped in the meantime) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_Tracing_ChannelProcessingLoopStartupSkipped")]
    private static partial void ChannelProcessingLoopStartupSkipped(ILogger logger, string channel, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Committing consumer transaction... | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionCommitting")]
    private static partial void ConsumerTransactionCommitting(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Consumer transaction committed. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionCommitted")]
    private static partial void ConsumerTransactionCommitted(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Not committing consumer transaction because it was already committed. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionAlreadyCommitted")]
    private static partial void ConsumerTransactionAlreadyCommitted(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Aborting consumer transaction... | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionAborting")]
    private static partial void ConsumerTransactionAborting(
        ILogger logger,
        string endpointName,
        string brokerMessageId,
        Exception? exception);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Consumer transaction aborted. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionAborted")]
    private static partial void ConsumerTransactionAborted(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Not aborting consumer transaction because it was already aborted. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ConsumerTransactionAlreadyAborted")]
    private static partial void ConsumerTransactionAlreadyAborted(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Processing canceled by the consumer stop: rolling back the transaction. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ProcessingCanceledByConsumerStop")]
    private static partial void ProcessingCanceledByConsumerStop(
        ILogger logger,
        string endpointName,
        string brokerMessageId,
        Exception exception);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "All policies have been applied but the message(s) couldn't be successfully processed. The client will be disconnected. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_AllErrorPoliciesApplied")]
    private static partial void AllErrorPoliciesApplied(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "The {PolicyType} will be skipped because the current failed attempts ({FailedAttempts}) exceeds the configured maximum attempts ({MaxFailedAttempts}). | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ErrorPolicySkippedMaxFailedAttempts")]
    private static partial void ErrorPolicySkippedMaxFailedAttempts(
        ILogger logger,
        string policyType,
        int failedAttempts,
        int? maxFailedAttempts,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "The {PolicyType} will be skipped because the {ExceptionType} is not in the list of handled exceptions. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ErrorPolicySkippedExceptionNotIncluded")]
    private static partial void ErrorPolicySkippedExceptionNotIncluded(
        ILogger logger,
        string policyType,
        string exceptionType,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "The {PolicyType} will be skipped because the {ExceptionType} is in the list of excluded exceptions. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ErrorPolicySkippedExceptionExcluded")]
    private static partial void ErrorPolicySkippedExceptionExcluded(
        ILogger logger,
        string policyType,
        string exceptionType,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "The {PolicyType} will be skipped because the apply rule evaluated to false. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_ErrorPolicySkippedApplyRule")]
    private static partial void ErrorPolicySkippedApplyRule(ILogger logger, string policyType, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Waiting {Delay} milliseconds before retrying to process the message(s). | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_RetryDelayWaiting")]
    private static partial void RetryDelayWaiting(ILogger logger, double delay, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Publishing {SequenceType} '{SequenceId}'... | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequencePublishing")]
    private static partial void SequencePublishing(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Published {SequenceType} '{SequenceId}' (ProcessingTask.Id={ProcessingTaskId}). | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequencePublished")]
    private static partial void SequencePublished(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        int processingTaskId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "No subscribers for {SequenceType} '{SequenceId}'. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceNoSubscribers")]
    private static partial void SequenceNoSubscribers(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "All {SequenceType} '{SequenceId}' subscribers completed. (sequence.IsCompleted={Completed}, faultedTasks={FaultedCount}/{TasksCount}).",
        EventName = "Silverback.Integration_Tracing_SequenceSubscribersCompleted")]
    private static partial void SequenceSubscribersCompleted(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        bool completed,
        int faultedCount,
        int tasksCount);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "{SequenceType} '{SequenceId}' processing completed. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceProcessingCompleted")]
    private static partial void SequenceProcessingCompleted(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "{SequenceType} '{SequenceId}' processing has completed but it's not the beginning of the outer sequence. No action will be performed. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceProcessingCompletedNotOuterStart")]
    private static partial void SequenceProcessingCompletedNotOuterStart(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "{SequenceType} '{SequenceId}' processing seems completed but the sequence is still pending: ProcessingTask.Status={ProcessingTaskStatus}, ProcessingTask.Id={ProcessingTaskId}) | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceStillPending")]
    private static partial void SequenceStillPending(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        TaskStatus? processingTaskStatus,
        int? processingTaskId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Sequence ended or aborted: awaiting processing task. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceEndedAwaitingProcessingTask")]
    private static partial void SequenceEndedAwaitingProcessingTask(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Sequence ended or aborted: processing task completed. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceEndedProcessingTaskCompleted")]
    private static partial void SequenceEndedProcessingTaskCompleted(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Awaiting {SequenceType} '{SequenceId}' processing task. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceProcessingTaskAwaiting")]
    private static partial void SequenceProcessingTaskAwaiting(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Successfully awaited {SequenceType} '{SequenceId}' processing task. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceProcessingTaskAwaited")]
    private static partial void SequenceProcessingTaskAwaited(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        string endpointName,
        string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Awaiting sequence processing completed before rethrowing. | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_Tracing_SequenceProcessingAwaitingBeforeRethrow")]
    private static partial void SequenceProcessingAwaitingBeforeRethrow(ILogger logger, string endpointName, string brokerMessageId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Operation canceled, skipping add to {SequenceType} '{SequenceId}' and waiting until it is completed.",
        EventName = "Silverback.Integration_Tracing_SequenceAddCanceled")]
    private static partial void SequenceAddCanceled(ILogger logger, string sequenceType, string sequenceId, Exception exception);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Error occurred adding message to {SequenceType} '{SequenceId}'.",
        EventName = "Silverback.Integration_Tracing_SequenceAddFailed")]
    private static partial void SequenceAddFailed(ILogger logger, string sequenceType, string sequenceId, Exception exception);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Completing {SequenceType} '{SequenceId}' (length {SequenceLength})...",
        EventName = "Silverback.Integration_Tracing_SequenceCompleting")]
    private static partial void SequenceCompleting(ILogger logger, string sequenceType, string sequenceId, int sequenceLength);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "{SequenceType} '{SequenceId}' is completing (total length {SequenceLength})...",
        EventName = "Silverback.Integration_Tracing_SequenceCompletingWithTotalLength")]
    private static partial void SequenceCompletingWithTotalLength(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        int? sequenceLength);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Timeout was elapsed for {SequenceType} '{SequenceId}', executing timeout logic...",
        EventName = "Silverback.Integration_Tracing_SequenceTimeoutElapsed")]
    private static partial void SequenceTimeoutElapsed(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Aborting {SequenceType} '{SequenceId}' ({AbortReason})...",
        EventName = "Silverback.Integration_Tracing_SequenceAborting")]
    private static partial void SequenceAborting(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        SequenceAbortReason abortReason,
        Exception? exception);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Aborting {SequenceType} '{SequenceId}' is waiting for add semaphore...",
        EventName = "Silverback.Integration_Tracing_SequenceAbortAwaitingAddSemaphore")]
    private static partial void SequenceAbortAwaitingAddSemaphore(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Disposing {SequenceType} '{SequenceId}'...",
        EventName = "Silverback.Integration_Tracing_SequenceDisposing")]
    private static partial void SequenceDisposing(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "{SequenceType} '{SequenceId}' disposed.",
        EventName = "Silverback.Integration_Tracing_SequenceDisposed")]
    private static partial void SequenceDisposed(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Dispose is waiting for the abort to complete ({SequenceType} '{SequenceId}')...",
        EventName = "Silverback.Integration_Tracing_SequenceDisposeAwaitingAbort")]
    private static partial void SequenceDisposeAwaitingAbort(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Dispose is waiting add semaphore ({SequenceType} '{SequenceId}')...",
        EventName = "Silverback.Integration_Tracing_SequenceDisposeAwaitingAddSemaphore")]
    private static partial void SequenceDisposeAwaitingAddSemaphore(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Dispose is waiting complete semaphore ({SequenceType} '{SequenceId}')...",
        EventName = "Silverback.Integration_Tracing_SequenceDisposeAwaitingCompleteSemaphore")]
    private static partial void SequenceDisposeAwaitingCompleteSemaphore(ILogger logger, string sequenceType, string sequenceId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Waiting for semaphore...",
        EventName = "Silverback.Integration_Tracing_SemaphoreWaiting")]
    private static partial void SemaphoreWaiting(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Semaphore acquired",
        EventName = "Silverback.Integration_Tracing_SemaphoreAcquired")]
    private static partial void SemaphoreAcquired(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Sequence is disposed, no need to wait for semaphore",
        EventName = "Silverback.Integration_Tracing_SemaphoreWaitSkippedSequenceDisposed")]
    private static partial void SemaphoreWaitSkippedSequenceDisposed(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Sequence disposed while waiting for semaphore",
        EventName = "Silverback.Integration_Tracing_SemaphoreWaitInterruptedSequenceDisposed")]
    private static partial void SemaphoreWaitInterruptedSequenceDisposed(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Object disposed exception while waiting for semaphore",
        EventName = "Silverback.Integration_Tracing_SemaphoreObjectDisposed")]
    private static partial void SemaphoreObjectDisposed(ILogger logger);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Adding {SequenceType} '{SequenceId}' to store '{SequenceStoreId}'.",
        EventName = "Silverback.Integration_Tracing_SequenceAddedToStore")]
    private static partial void SequenceAddedToStore(ILogger logger, string sequenceType, string sequenceId, Guid sequenceStoreId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Removing sequence '{SequenceId}' from store '{SequenceStoreId}'.",
        EventName = "Silverback.Integration_Tracing_SequenceRemovedFromStore")]
    private static partial void SequenceRemovedFromStore(ILogger logger, string sequenceId, Guid sequenceStoreId);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Disposing sequence store {SequenceStoreId}",
        EventName = "Silverback.Integration_Tracing_SequenceStoreDisposing")]
    private static partial void SequenceStoreDisposing(ILogger logger, Guid sequenceStoreId);
}
