// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Validation;

namespace Silverback.Diagnostics;

// The IntegrationLogEvents properties are generated from the [LoggerMessage] methods, including their documentation
internal static partial class IntegrationLoggerExtensions
{
    public static void LogProcessing(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessage))
            return;

        ProcessingConsumedMessage(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogProcessingError(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageError))
            return;

        ProcessingConsumedMessageError(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogProcessingFatalError(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageFatalError))
            return;

        ProcessingConsumedMessageFatalError(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogConsumerFatalError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerFatalError(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogProduced(this ISilverbackLogger logger, IOutboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
            return;

        MessageProduced(
            logger.InnerLogger,
            envelope.GetEndpoint().DisplayName,
            envelope.BrokerMessageIdentifier?.ToLogString());
    }

    public static void LogProduceError(this ISilverbackLogger logger, IOutboundEnvelope envelope, Exception exception) =>
        ErrorProducingMessage(logger.InnerLogger, envelope.EndpointConfiguration.DisplayName, exception);

    public static void LogProduceError(this ISilverbackLogger logger, ProducerEndpointConfiguration endpointConfiguration, Exception exception) =>
        ErrorProducingMessage(logger.InnerLogger, endpointConfiguration.DisplayName, exception);

    public static void LogOutboundMessageFiltered(this ISilverbackLogger logger, IOutboundEnvelope envelope) =>
        OutboundMessageFiltered(logger.InnerLogger, envelope.EndpointConfiguration.DisplayName);

    public static void LogMessageAddedToSequence(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageAddedToSequence))
            return;

        MessageAddedToSequence(
            logger.InnerLogger,
            envelope.BrokerMessageIdentifier.ToLogString(),
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length);
    }

    public static void LogSequenceStarted(
        this ISilverbackLogger logger,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceStarted))
            return;

        SequenceStarted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId);
    }

    public static void LogSequenceCompleted(
        this ISilverbackLogger logger,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceCompleted))
            return;

        SequenceCompleted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length);
    }

    public static void LogSequenceAborted(
        this ISilverbackLogger logger,
        ISequence sequence,
        SequenceAbortReason reason)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceProcessingAborted))
            return;

        SequenceProcessingAborted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            reason);
    }

    public static void LogSequenceProcessingError(this ISilverbackLogger logger, ISequence sequence, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceProcessingError))
            return;

        SequenceProcessingError(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            exception);
    }

    public static void LogIncompleteSequenceAborted(this ISilverbackLogger logger, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.IncompleteSequenceAborted))
            return;

        IncompleteSequenceAborted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length);
    }

    public static void LogIncompleteSequenceSkipped(this ISilverbackLogger logger, IncompleteSequence sequence) =>
        IncompleteSequenceSkipped(logger.InnerLogger, sequence.SequenceId);

    public static void LogSequenceTimeoutError(this ISilverbackLogger logger, ISequence sequence, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceTimeoutError))
            return;

        SequenceTimeoutError(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, exception);
    }

    public static void LogBrokerClientsInitializationError(this ISilverbackLogger logger, Exception exception) =>
        BrokerClientsInitializationError(logger.InnerLogger, exception);

    public static void LogBrokerClientInitializing(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientInitializing(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName);

    public static void LogBrokerClientInitialized(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientInitialized(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName);

    public static void LogBrokerClientDisconnecting(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientDisconnecting(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName);

    public static void LogBrokerClientDisconnected(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientDisconnected(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName);

    public static void LogBrokerClientInitializeError(this ISilverbackLogger logger, IBrokerClient brokerClient, Exception exception) =>
        BrokerClientInitializeError(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, exception);

    public static void LogBrokerClientDisconnectError(this ISilverbackLogger logger, IBrokerClient brokerClient, Exception exception) =>
        BrokerClientDisconnectError(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, exception);

    public static void LogBrokerClientReconnectError(
        this ISilverbackLogger logger,
        IBrokerClient brokerClient,
        TimeSpan retryDelay,
        Exception exception) =>
        BrokerClientReconnectError(
            logger.InnerLogger,
            brokerClient.GetType().Name,
            retryDelay.TotalMilliseconds,
            brokerClient.DisplayName,
            exception);

    public static void LogConsumerStartError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerStartError(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName, exception);

    public static void LogConsumerStopError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerStopError(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName, exception);

    public static void LogConsumerCommitError(
        this ISilverbackLogger logger,
        IConsumer consumer,
        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ConsumerCommitError))
            return;

        ConsumerCommitError(
            logger.InnerLogger,
            consumer.GetType().Name,
            consumer.DisplayName,
            string.Join(", ", identifiers.Select(identifier => identifier.ToVerboseLogString())),
            exception);
    }

    public static void LogConsumerRollbackError(
        this ISilverbackLogger logger,
        IConsumer consumer,
        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ConsumerRollbackError))
            return;

        ConsumerRollbackError(
            logger.InnerLogger,
            consumer.GetType().Name,
            consumer.DisplayName,
            string.Join(", ", identifiers.Select(identifier => identifier.ToVerboseLogString())),
            exception);
    }

    public static void LogBrokerClientCreated(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientCreated(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName);

    public static void LogConsumerCreated(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerCreated(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName);

    public static void LogProducerCreated(this ISilverbackLogger logger, IProducer producer) =>
        ProducerCreated(logger.InnerLogger, producer.GetType().Name, producer.DisplayName);

    public static void LogRetryProcessing(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RetryMessageProcessing))
            return;

        RetryMessageProcessing(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogMessageMoved(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        EndpointConfiguration destinationEndpoint)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageMoved))
            return;

        MessageMoved(
            logger.InnerLogger,
            destinationEndpoint.DisplayName,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogMessageSkipped(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageSkipped))
            return;

        MessageSkipped(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogCannotMoveSequence(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.CannotMoveSequence))
            return;

        CannotMoveSequence(
            logger.InnerLogger,
            sequence.GetType().Name,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogRollbackToRetryFailed(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToRetryFailed))
            return;

        RollbackToRetryFailed(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogRollbackToSkipFailed(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToSkipFailed))
            return;

        RollbackToSkipFailed(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogStoringIntoOutbox(this ISilverbackLogger logger, IOutboundEnvelope envelope) =>
        StoringIntoOutbox(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName);

    public static void LogReadingMessagesFromOutbox(this ISilverbackLogger logger, int packageSize) =>
        ReadingMessagesFromOutbox(logger.InnerLogger, packageSize);

    public static void LogOutboxEmpty(this ISilverbackLogger logger) =>
        OutboxEmpty(logger.InnerLogger);

    public static void LogProcessingOutboxStoredMessage(this ISilverbackLogger logger, int currentIndex) =>
        ProcessingOutboxStoredMessage(logger.InnerLogger, currentIndex);

    public static void LogErrorProducingOutboxStoredMessage(
        this ISilverbackLogger logger,
        OutboxMessage message,
        Exception exception) =>
        ErrorProducingOutboxStoredMessage(logger.InnerLogger, message.EndpointName, exception);

    public static void LogErrorProcessingOutbox(this ISilverbackLogger logger, Exception exception) =>
        ErrorProcessingOutbox(logger.InnerLogger, exception);

    public static void LogInvalidMessageProduced(
        this ISilverbackLogger logger,
        IOutboundEnvelope envelope,
        string validationErrors) =>
        InvalidMessageProduced(
            logger.InnerLogger,
            validationErrors,
            envelope.EndpointConfiguration.DisplayName);

    public static void LogInvalidMessageConsumed(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string validationErrors)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.InvalidMessageConsumed))
            return;

        InvalidMessageConsumed(
            logger.InnerLogger,
            validationErrors,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString());
    }

    public static void LogInvalidEndpointConfiguration(
        this ISilverbackLogger logger,
        EndpointConfiguration endpointConfiguration,
        Exception exception) =>
        InvalidEndpointConfiguration(logger.InnerLogger, endpointConfiguration.DisplayName, exception);

    public static void LogEndpointConfiguratorError(
        this ISilverbackLogger logger,
        IBrokerClientsConfigurator configurator,
        Exception exception) =>
        EndpointConfiguratorError(logger.InnerLogger, configurator.GetType().Name, exception);

    public static void LogCallbackError(this ISilverbackLogger logger, Exception exception) =>
        CallbackError(logger.InnerLogger, exception);

    public static void LogEndpointBuilderError(this ISilverbackLogger logger, string? endpointName, Exception exception) =>
        EndpointBuilderError(logger.InnerLogger, endpointName, exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a consumed message is being processed.
    /// </summary>
    [LoggerMessage(
        1001,
        LogLevel.Information,
        "Processing consumed message | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_ProcessingConsumedMessage")]
    private static partial void ProcessingConsumedMessage(ILogger logger, string endpointName, string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs processing the consumed message.
    /// </summary>
    /// <remarks>
    ///     If the message belongs to a sequence the <see cref="SequenceProcessingError" /> event is logged instead.
    /// </remarks>
    [LoggerMessage(
        1002,
        LogLevel.Error,
        "Error occurred processing consumed message | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_ProcessingConsumedMessageError")]
    private static partial void ProcessingConsumedMessageError(
        ILogger logger,
        string endpointName,
        string brokerMessageId,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an unhandled error occurs processing the
    ///     consumed message and the consumer will be stopped.
    /// </summary>
    [LoggerMessage(
        1003,
        LogLevel.Critical,
        "Fatal error occurred processing consumed message; the client will be disconnected | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_ProcessingConsumedMessageFatalError")]
    private static partial void ProcessingConsumedMessageFatalError(
        ILogger logger,
        string endpointName,
        string brokerMessageId,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log written when an error occurs processing the consumed message, and
    ///     it isn't handled even by the <see cref="FatalExceptionLoggerConsumerBehavior" /> (which should never happen).
    /// </summary>
    [LoggerMessage(
        1004,
        LogLevel.Critical,
        "Fatal error occurred processing consumed message; the client will be disconnected | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_ConsumerFatalError")]
    private static partial void ConsumerFatalError(ILogger logger, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is produced.
    /// </summary>
    [LoggerMessage(
        1005,
        LogLevel.Information,
        "Message produced | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_MessageProduced")]
    private static partial void MessageProduced(ILogger logger, string endpointName, string? brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs producing a message.
    /// </summary>
    [LoggerMessage(
        1006,
        LogLevel.Warning,
        "Error occurred producing message | EndpointName: {EndpointName}",
        EventName = "Silverback.Integration_ErrorProducingMessage")]
    private static partial void ErrorProducingMessage(ILogger logger, string endpointName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an outbound message is filtered out.
    /// </summary>
    [LoggerMessage(
        1007,
        LogLevel.Debug,
        "Message filtered | EndpointName: {EndpointName}",
        EventName = "Silverback.Integration_OutboundMessageFiltered")]
    private static partial void OutboundMessageFiltered(ILogger logger, string endpointName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an inbound message is added to
    ///     a sequence (e.g. <see cref="ChunkSequence" /> or a <see cref="BatchSequence" />).
    /// </summary>
    [LoggerMessage(
        1011,
        LogLevel.Debug,
        "Message {BrokerMessageId} added to {SequenceType} {SequenceId} | Length: {SequenceLength}",
        EventName = "Silverback.Integration_MessageAddedToSequence")]
    private static partial void MessageAddedToSequence(
        ILogger logger,
        string? brokerMessageId,
        string sequenceType,
        string sequenceId,
        int sequenceLength);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the first message of a new
    ///     sequence is consumed.
    /// </summary>
    [LoggerMessage(
        1012,
        LogLevel.Debug,
        "Started new {SequenceType} {SequenceId}",
        EventName = "Silverback.Integration_SequenceStarted")]
    private static partial void SequenceStarted(ILogger logger, string sequenceType, string sequenceId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when all messages belonging to the
    ///     sequence have been consumed and published via the message bus.
    /// </summary>
    [LoggerMessage(
        1013,
        LogLevel.Debug,
        "{SequenceType} {SequenceId} completed | Length: {SequenceLength}",
        EventName = "Silverback.Integration_SequenceCompleted")]
    private static partial void SequenceCompleted(ILogger logger, string sequenceType, string sequenceId, int sequenceLength);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of a sequence is aborted,
    ///     but not because of an error (an <see cref="SequenceProcessingError" /> is logged instead) or an incomplete sequence that gets
    ///     discarded (an <see cref="IncompleteSequenceAborted" /> or an <see cref="IncompleteSequenceSkipped" /> is logged instead).
    /// </summary>
    [LoggerMessage(
        1014,
        LogLevel.Debug,
        "{SequenceType} {SequenceId} processing has been aborted | Length: {SequenceLength}, Reason: {Reason}",
        EventName = "Silverback.Integration_SequenceProcessingAborted")]
    private static partial void SequenceProcessingAborted(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        int sequenceLength,
        SequenceAbortReason reason);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs processing an inbound sequence.
    /// </summary>
    [LoggerMessage(
        1015,
        LogLevel.Error,
        "Error occurred processing {SequenceType} {SequenceId} | Length: {SequenceLength}",
        EventName = "Silverback.Integration_SequenceProcessingError")]
    private static partial void SequenceProcessingError(
        ILogger logger,
        string sequenceType,
        string sequenceId,
        int sequenceLength,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log written when a sequence is aborted because a new one starts before it
    ///     completes, or because it times out before completion.
    /// </summary>
    [LoggerMessage(
        1016,
        LogLevel.Warning,
        "Aborted incomplete {SequenceType} {SequenceId} | Length: {SequenceLength}",
        EventName = "Silverback.Integration_IncompleteSequenceAborted")]
    private static partial void IncompleteSequenceAborted(ILogger logger, string sequenceType, string sequenceId, int sequenceLength);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an incomplete sequence is  skipped because the first
    ///     consumed message of that sequence doesn't correspond to the actual sequence beginning (e.g., the first chunk).
    /// </summary>
    [LoggerMessage(
        1017,
        LogLevel.Warning,
        "Skipped incomplete sequence {SequenceId} (missing first message)",
        EventName = "Silverback.Integration_IncompleteSequenceSkipped")]
    private static partial void IncompleteSequenceSkipped(ILogger logger, string sequenceId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs executing the timeout action on a
    ///     sequence.
    /// </summary>
    [LoggerMessage(
        1018,
        LogLevel.Warning,
        "Error occurred executing timeout for {SequenceType} {SequenceId}",
        EventName = "Silverback.Integration_SequenceTimeoutError")]
    private static partial void SequenceTimeoutError(ILogger logger, string sequenceType, string sequenceId, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when initializing the
    ///     broker clients.
    /// </summary>
    [LoggerMessage(
        1021,
        LogLevel.Error,
        "Error occurred initializing broker clients",
        EventName = "Silverback.Integration_BrokerClientsInitializationError")]
    private static partial void BrokerClientsInitializationError(ILogger logger, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the broker client is initializing.
    /// </summary>
    [LoggerMessage(
        1022,
        LogLevel.Debug,
        "{ClientType} initializing | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientInitializing")]
    private static partial void BrokerClientInitializing(ILogger logger, string clientType, string clientName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the broker client has been successfully initialized.
    ///     The connection with the broker will eventually be established.
    /// </summary>
    [LoggerMessage(
        1023,
        LogLevel.Debug,
        "{ClientType} initialized | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientInitialized")]
    private static partial void BrokerClientInitialized(ILogger logger, string clientType, string clientName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnecting from the message
    ///     broker.
    /// </summary>
    [LoggerMessage(
        1024,
        LogLevel.Debug,
        "{ClientType} disconnecting | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientDisconnecting")]
    private static partial void BrokerClientDisconnecting(ILogger logger, string clientType, string clientName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnected from the message
    ///     broker.
    /// </summary>
    [LoggerMessage(
        1025,
        LogLevel.Information,
        "{ClientType} disconnected | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientDisconnected")]
    private static partial void BrokerClientDisconnected(ILogger logger, string clientType, string clientName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when initializing the broker
    ///     client.
    /// </summary>
    [LoggerMessage(
        1026,
        LogLevel.Error,
        "Error occurred initializing {ClientType} | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientInitializeError")]
    private static partial void BrokerClientInitializeError(ILogger logger, string clientType, string clientName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when disconnecting the broker
    ///     client.
    /// </summary>
    [LoggerMessage(
        1027,
        LogLevel.Error,
        "Error occurred disconnecting {ClientType} | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientDisconnectError")]
    private static partial void BrokerClientDisconnectError(ILogger logger, string clientType, string clientName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown reconnecting the broker client
    ///     (auto recovery from errors).
    /// </summary>
    [LoggerMessage(
        1028,
        LogLevel.Warning,
        "Failed to reconnect {ClientType}; retry in {RetryDelay} ms | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientReconnectError")]
    private static partial void BrokerClientReconnectError(
        ILogger logger,
        string clientType,
        double retryDelay,
        string clientName,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
    ///     starting the consumer.
    /// </summary>
    [LoggerMessage(
        1031,
        LogLevel.Error,
        "Error occurred (re)starting {ConsumerType} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_ConsumerStartError")]
    private static partial void ConsumerStartError(ILogger logger, string consumerType, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
    ///     stopping the consumer.
    /// </summary>
    [LoggerMessage(
        1032,
        LogLevel.Error,
        "Error occurred stopping {ConsumerType} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_ConsumerStopError")]
    private static partial void ConsumerStopError(ILogger logger, string consumerType, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
    ///     consumer during the commit operation.
    /// </summary>
    [LoggerMessage(
        1033,
        LogLevel.Error,
        "{ConsumerType} commit failed | ConsumerName: {ConsumerName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_ConsumerCommitError")]
    private static partial void ConsumerCommitError(
        ILogger logger,
        string consumerType,
        string consumerName,
        string brokerMessageId,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
    ///     consumer during the rollback operation.
    /// </summary>
    [LoggerMessage(
        1034,
        LogLevel.Error,
        "{ConsumerType} rollback failed | ConsumerName: {ConsumerName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_ConsumerRollbackError")]
    private static partial void ConsumerRollbackError(
        ILogger logger,
        string consumerType,
        string consumerName,
        string brokerMessageId,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new broker client is instantiated.
    /// </summary>
    [LoggerMessage(
        1041,
        LogLevel.Debug,
        "Created {ClientType} | ClientName: {ClientName}",
        EventName = "Silverback.Integration_BrokerClientCreated")]
    private static partial void BrokerClientCreated(ILogger logger, string clientType, string clientName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer is instantiated.
    /// </summary>
    [LoggerMessage(
        1042,
        LogLevel.Debug,
        "Created {ConsumerType} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration_ConsumerCreated")]
    private static partial void ConsumerCreated(ILogger logger, string consumerType, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer is instantiated.
    /// </summary>
    [LoggerMessage(
        1043,
        LogLevel.Debug,
        "Created {ProducerType} | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration_ProducerCreated")]
    private static partial void ProducerCreated(ILogger logger, string producerType, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be processed and the
    ///     <see cref="RetryErrorPolicy" /> is applied.
    /// </summary>
    [LoggerMessage(
        1051,
        LogLevel.Information,
        "The message(s) will be processed again | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_RetryMessageProcessing")]
    private static partial void RetryMessageProcessing(ILogger logger, string endpointName, string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
    ///     processed and is moved to another endpoint. This event occurs when the
    ///     <see cref="MoveMessageErrorPolicy" /> is applied.
    /// </summary>
    [LoggerMessage(
        1052,
        LogLevel.Information,
        "The message will be moved to {TargetEndpointName} | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_MessageMoved")]
    private static partial void MessageMoved(ILogger logger, string targetEndpointName, string endpointName, string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
    ///     processed and is skipped. This event occurs when the <see cref="SkipMessageErrorPolicy" /> is applied.
    /// </summary>
    [LoggerMessage(
        1053,
        LogLevel.Information,
        "The message(s) will be skipped | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_MessageSkipped")]
    private static partial void MessageSkipped(ILogger logger, string endpointName, string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="MoveMessageErrorPolicy" /> cannot be applied because the failing message belongs to a
    ///     sequence (it's either chunked, being processed in batch, etc.).
    /// </summary>
    [LoggerMessage(
        1054,
        LogLevel.Warning,
        "The message belongs to a {SequenceType} and cannot be moved | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_CannotMoveSequence")]
    private static partial void CannotMoveSequence(ILogger logger, string sequenceType, string endpointName, string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a rollback initiated by
    ///     the <see cref="RetryErrorPolicy" /> fails with an exception. This will cause the consumer to be
    ///     disconnected and reconnected.
    /// </summary>
    [LoggerMessage(
        1061,
        LogLevel.Warning,
        "Error occurred rolling back, retry error policy cannot be applied; the consumer will be reconnected " +
        "| EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_RollbackToRetryFailed")]
    private static partial void RollbackToRetryFailed(ILogger logger, string endpointName, string brokerMessageId, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a rollback initiated by
    ///     the <see cref="SkipMessageErrorPolicy" /> fails with an exception. This will cause the consumer to be
    ///     disconnected and reconnected.
    /// </summary>
    [LoggerMessage(
        1062,
        LogLevel.Warning,
        "Error occurred rolling back transaction or committing offset, skip error policy cannot be applied; " +
        "the consumer will be reconnected | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_RollbackToSkipFailed")]
    private static partial void RollbackToSkipFailed(ILogger logger, string endpointName, string brokerMessageId, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is being written
    ///     to the outbox.
    /// </summary>
    [LoggerMessage(
        1071,
        LogLevel.Debug,
        "Storing message into outbox | EndpointName: {EndpointName}",
        EventName = "Silverback.Integration_StoringIntoOutbox")]
    private static partial void StoringIntoOutbox(ILogger logger, string endpointName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="IOutboxWorker" /> loads a batch of enqueued messages.
    /// </summary>
    [LoggerMessage(
        1072,
        LogLevel.Trace,
        "Reading batch of {ReadBatchSize} messages from outbox",
        EventName = "Silverback.Integration_ReadingMessagesFromOutbox")]
    private static partial void ReadingMessagesFromOutbox(ILogger logger, int readBatchSize);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="IOutboxWorker" /> doesn't find any message in the outbox.
    /// </summary>
    [LoggerMessage(
        1073,
        LogLevel.Trace,
        "Outbox empty",
        EventName = "Silverback.Integration_OutboxEmpty")]
    private static partial void OutboxEmpty(ILogger logger);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message stored in the outbox is being processed.
    /// </summary>
    [LoggerMessage(
        1074,
        LogLevel.Debug,
        "Processing outbox message {CurrentMessageIndex}",
        EventName = "Silverback.Integration_ProcessingOutboxStoredMessage")]
    private static partial void ProcessingOutboxStoredMessage(ILogger logger, int currentMessageIndex);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs  producing the message stored in the
    ///     outbox.
    /// </summary>
    [LoggerMessage(
        1075,
        LogLevel.Error,
        "Failed to produce message from outbox | EndpointName: {EndpointName}",
        EventName = "Silverback.Integration_ErrorProducingOutboxStoredMessage")]
    private static partial void ErrorProducingOutboxStoredMessage(ILogger logger, string endpointName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while the <see cref="IOutboxWorker" />
    ///     processes the outbox queue.
    /// </summary>
    [LoggerMessage(
        1076,
        LogLevel.Error,
        "Error occurred processing outbox",
        EventName = "Silverback.Integration_ErrorProcessingOutbox")]
    private static partial void ErrorProcessingOutbox(ILogger logger, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an invalid message
    ///     has been produced (see <see cref="MessageValidationMode" />).
    /// </summary>
    [LoggerMessage(
        1081,
        LogLevel.Warning,
        "Invalid message produced: {ValidationErrors} | EndpointName: {EndpointName}",
        EventName = "Silverback.Integration_InvalidMessageProduced")]
    private static partial void InvalidMessageProduced(ILogger logger, string validationErrors, string endpointName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an invalid message
    ///     has been consumed (see <see cref="MessageValidationMode" />).
    /// </summary>
    [LoggerMessage(
        1082,
        LogLevel.Warning,
        "Invalid message consumed: {ValidationErrors} | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
        EventName = "Silverback.Integration_InvalidMessageConsumed")]
    private static partial void InvalidMessageConsumed(
        ILogger logger,
        string validationErrors,
        string endpointName,
        string brokerMessageId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when trying to connect an endpoint
    ///     with an invalid configuration.
    /// </summary>
    [LoggerMessage(
        1101,
        LogLevel.Critical,
        "Invalid configuration for endpoint {EndpointName}",
        EventName = "Silverback.Integration_InvalidEndpointConfiguration")]
    private static partial void InvalidEndpointConfiguration(ILogger logger, string endpointName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown from
    ///     within an <see cref="IBrokerClientsConfigurator" />.
    /// </summary>
    [LoggerMessage(
        1102,
        LogLevel.Critical,
        "Error occurred configuring endpoints | Configurator: {EndpointsConfiguratorName}",
        EventName = "Silverback.Integration_EndpointConfiguratorError")]
    private static partial void EndpointConfiguratorError(ILogger logger, string endpointsConfiguratorName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown by a client callback handler.
    /// </summary>
    [LoggerMessage(
        1103,
        LogLevel.Error,
        "Error occurred invoking callback handlers",
        EventName = "Silverback.Integration_CallbackError")]
    private static partial void CallbackError(ILogger logger, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown by the endpoint builder action.
    /// </summary>
    [LoggerMessage(
        1104,
        LogLevel.Critical,
        "Failed to configure endpoint {EndpointName}",
        EventName = "Silverback.Integration_EndpointBuilderError")]
    private static partial void EndpointBuilderError(ILogger logger, string? endpointName, Exception exception);
}
