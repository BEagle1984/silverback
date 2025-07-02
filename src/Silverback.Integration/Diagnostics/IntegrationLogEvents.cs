// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Validation;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the Silverback.Integration
///     package.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Cleaner and clearer this way")]
public static class IntegrationLogEvents
{
    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a consumed message is being processed.
    /// </summary>
    public static LogEvent ProcessingConsumedMessage { get; } = new(
        LogLevel.Information,
        GetEventId(1, nameof(ProcessingConsumedMessage)),
        "Processing consumed message. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs processing the consumed message.
    /// </summary>
    /// <remarks>
    ///     If the message belongs to a sequence the <see cref="SequenceProcessingError" /> event is logged instead.
    /// </remarks>
    public static LogEvent ProcessingConsumedMessageError { get; } = new(
        LogLevel.Error,
        GetEventId(2, nameof(ProcessingConsumedMessageError)),
        "Error occurred processing the consumed message. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an unhandled error occurs processing the
    ///     consumed message and the consumer will be stopped.
    /// </summary>
    public static LogEvent ProcessingConsumedMessageFatalError { get; } = new(
        LogLevel.Critical,
        GetEventId(3, nameof(ProcessingConsumedMessageFatalError)),
        "Fatal error occurred processing the consumed message. The client will be disconnected. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log written when an error occurs processing the consumed message, and
    ///     it isn't handled even by the <see cref="FatalExceptionLoggerConsumerBehavior" /> (which should never happen).
    /// </summary>
    public static LogEvent ConsumerFatalError { get; } = new(
        LogLevel.Critical,
        GetEventId(4, nameof(ConsumerFatalError)),
        "Fatal error occurred processing the consumed message. The client will be disconnected. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is produced.
    /// </summary>
    public static LogEvent MessageProduced { get; } = new(
        LogLevel.Information,
        GetEventId(5, nameof(MessageProduced)),
        "Message produced. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs producing a message.
    /// </summary>
    public static LogEvent ErrorProducingMessage { get; } = new(
        LogLevel.Warning,
        GetEventId(6, nameof(ErrorProducingMessage)),
        "Error occurred producing the message. | endpointName: {endpointName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an outbound message is filtered out.
    /// </summary>
    public static LogEvent OutboundMessageFiltered { get; } = new(
        LogLevel.Debug,
        GetEventId(7, nameof(OutboundMessageFiltered)),
        "Message filtered. | endpointName: {endpointName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an inbound message is added to
    ///     a sequence (e.g. <see cref="ChunkSequence" /> or a <see cref="BatchSequence" />).
    /// </summary>
    public static LogEvent MessageAddedToSequence { get; } = new(
        LogLevel.Debug,
        GetEventId(11, nameof(MessageAddedToSequence)),
        "Message '{messageId}' added to {sequenceType} '{sequenceId}'. | length: {sequenceLength}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the first message of a new
    ///     sequence is consumed.
    /// </summary>
    public static LogEvent SequenceStarted { get; } = new(
        LogLevel.Debug,
        GetEventId(12, nameof(SequenceStarted)),
        "Started new {sequenceType} '{sequenceId}'.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when all messages belonging to the
    ///     sequence have been consumed and published via the message bus.
    /// </summary>
    public static LogEvent SequenceCompleted { get; } = new(
        LogLevel.Debug,
        GetEventId(13, nameof(SequenceCompleted)),
        "{sequenceType} '{sequenceId}' completed. | length: {sequenceLength}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of a sequence is aborted,
    ///     but not because of an error (an <see cref="SequenceProcessingError" /> is logged instead) or an incomplete sequence that gets
    ///     discarded (an <see cref="IncompleteSequenceAborted" /> or an <see cref="IncompleteSequenceSkipped" /> is logged instead).
    /// </summary>
    public static LogEvent SequenceProcessingAborted { get; } = new(
        LogLevel.Debug,
        GetEventId(14, nameof(SequenceProcessingAborted)),
        "The {sequenceType} '{sequenceId}' processing has been aborted. | " +
        "length: {sequenceLength}, reason: {reason}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs processing an inbound sequence.
    /// </summary>
    public static LogEvent SequenceProcessingError { get; } = new(
        LogLevel.Error,
        GetEventId(15, nameof(SequenceProcessingError)),
        "Error occurred processing the {sequenceType} '{sequenceId}'. | " +
        "length: {sequenceLength}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log written when a sequence is aborted because a new one starts before it
    ///     completes, or because it times out before completion.
    /// </summary>
    public static LogEvent IncompleteSequenceAborted { get; } = new(
        LogLevel.Warning,
        GetEventId(16, nameof(IncompleteSequenceAborted)),
        "Aborted incomplete {sequenceType} '{sequenceId}'. | " +
        "length: {sequenceLength}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an incomplete sequence is  skipped because the first
    ///     consumed message of that sequence doesn't correspond to the actual sequence beginning (e.g., the first chunk).
    /// </summary>
    public static LogEvent IncompleteSequenceSkipped { get; } = new(
        LogLevel.Warning,
        GetEventId(17, nameof(IncompleteSequenceSkipped)),
        "Skipped incomplete sequence '{sequenceId}'. The first message is missing.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs executing the timeout action on a
    ///     sequence.
    /// </summary>
    public static LogEvent SequenceTimeoutError { get; } = new(
        LogLevel.Warning,
        GetEventId(18, nameof(SequenceTimeoutError)),
        "Error occurred executing the timeout for the {sequenceType} '{sequenceId}'.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when initializing the
    ///     broker clients.
    /// </summary>
    public static LogEvent BrokerClientsInitializationError { get; } = new(
        LogLevel.Error,
        GetEventId(21, nameof(BrokerClientsInitializationError)),
        "Error occurred initializing the broker client(s).");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the broker client is initializing.
    /// </summary>
    public static LogEvent BrokerClientInitializing { get; } = new(
        LogLevel.Debug,
        GetEventId(22, nameof(BrokerClientInitializing)),
        "{clientType} initializing... | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the broker client has been successfully initialized.
    ///     The connection with the broker will eventually be established.
    /// </summary>
    public static LogEvent BrokerClientInitialized { get; } = new(
        LogLevel.Debug,
        GetEventId(23, nameof(BrokerClientInitialized)),
        "{clientType} initialized. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnecting from the message
    ///     broker.
    /// </summary>
    public static LogEvent BrokerClientDisconnecting { get; } = new(
        LogLevel.Debug,
        GetEventId(24, nameof(BrokerClientDisconnecting)),
        "{clientType} disconnecting... | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnected from the message
    ///     broker.
    /// </summary>
    public static LogEvent BrokerClientDisconnected { get; } = new(
        LogLevel.Information,
        GetEventId(25, nameof(BrokerClientDisconnected)),
        "{clientType} disconnected. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when initializing the broker
    ///     client.
    /// </summary>
    public static LogEvent BrokerClientInitializeError { get; } = new(
        LogLevel.Error,
        GetEventId(26, nameof(BrokerClientInitializeError)),
        "Error occurred initializing {clientType}. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when disconnecting the broker
    ///     client.
    /// </summary>
    public static LogEvent BrokerClientDisconnectError { get; } = new(
        LogLevel.Error,
        GetEventId(27, nameof(BrokerClientDisconnectError)),
        "Error occurred disconnecting {clientType}. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown reconnecting the broker client
    ///     (auto recovery from errors).
    /// </summary>
    public static LogEvent BrokerClientReconnectError { get; } = new(
        LogLevel.Warning,
        GetEventId(28, nameof(BrokerClientReconnectError)),
        "Failed to reconnect the {clientType}. Will retry in {retryDelay} milliseconds. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
    ///     starting the consumer.
    /// </summary>
    public static LogEvent ConsumerStartError { get; } = new(
        LogLevel.Error,
        GetEventId(31, nameof(ConsumerStartError)),
        "Error occurred (re)starting the {consumerType}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
    ///     stopping the consumer.
    /// </summary>
    public static LogEvent ConsumerStopError { get; } = new(
        LogLevel.Error,
        GetEventId(32, nameof(ConsumerStopError)),
        "Error occurred stopping the {consumerType}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
    ///     consumer during the commit operation.
    /// </summary>
    public static LogEvent ConsumerCommitError { get; } = new(
        LogLevel.Error,
        GetEventId(33, nameof(ConsumerCommitError)),
        "{consumerType} commit failed. | consumerName: {consumerName}, identifiers: {identifiers}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
    ///     consumer during the rollback operation.
    /// </summary>
    public static LogEvent ConsumerRollbackError { get; } = new(
        LogLevel.Error,
        GetEventId(34, nameof(ConsumerRollbackError)),
        "{consumerType} rollback failed. | consumerName: {consumerName}, identifiers: {identifiers}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new broker client is instantiated.
    /// </summary>
    public static LogEvent BrokerClientCreated { get; } = new(
        LogLevel.Debug,
        GetEventId(41, nameof(BrokerClientCreated)),
        "Created {clientType}. | clientName: {clientName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer is instantiated.
    /// </summary>
    public static LogEvent ConsumerCreated { get; } = new(
        LogLevel.Debug,
        GetEventId(42, nameof(ConsumerCreated)),
        "Created {consumerType}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer is instantiated.
    /// </summary>
    public static LogEvent ProducerCreated { get; } = new(
        LogLevel.Debug,
        GetEventId(43, nameof(ProducerCreated)),
        "Created {producerType}. | producerName: {producerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be processed and the
    ///     <see cref="RetryErrorPolicy" /> is applied.
    /// </summary>
    public static LogEvent RetryMessageProcessing { get; } = new(
        LogLevel.Information,
        GetEventId(51, nameof(RetryMessageProcessing)),
        "The message(s) will be processed again. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
    ///     processed and is moved to another endpoint. This event occurs when the
    ///     <see cref="MoveMessageErrorPolicy" /> is applied.
    /// </summary>
    public static LogEvent MessageMoved { get; } = new(
        LogLevel.Information,
        GetEventId(52, nameof(MessageMoved)),
        "The message will be moved to the endpoint '{targetEndpointName}'. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
    ///     processed and is skipped. This event occurs when the <see cref="SkipMessageErrorPolicy" /> is applied.
    /// </summary>
    public static LogEvent MessageSkipped { get; } = new(
        LogLevel.Information,
        GetEventId(53, nameof(MessageSkipped)),
        "The message(s) will be skipped. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="MoveMessageErrorPolicy" /> cannot be applied because the failing message belongs to a
    ///     sequence (it's either chunked, being processed in batch, etc.).
    /// </summary>
    public static LogEvent CannotMoveSequence { get; } = new(
        LogLevel.Warning,
        GetEventId(54, nameof(CannotMoveSequence)),
        "The message belongs to a {sequenceType} and cannot be moved. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a rollback initiated by
    ///     the <see cref="RetryErrorPolicy" /> fails with an exception. This will cause the consumer to be
    ///     disconnected and reconnected.
    /// </summary>
    public static LogEvent RollbackToRetryFailed { get; } = new(
        LogLevel.Warning,
        GetEventId(61, nameof(RollbackToRetryFailed)),
        "Error occurred rolling back, the retry error policy cannot be applied. The consumer will be reconnected. " +
        "| endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a rollback initiated by
    ///     the <see cref="SkipMessageErrorPolicy" /> fails with an exception. This will cause the consumer to be
    ///     disconnected and reconnected.
    /// </summary>
    public static LogEvent RollbackToSkipFailed { get; } = new(
        LogLevel.Warning,
        GetEventId(62, nameof(RollbackToSkipFailed)),
        "Error occurred rolling back the transaction or committing the offset, the skip message error policy cannot be applied. " +
        "The consumer will be reconnected. | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is being written
    ///     to the outbox.
    /// </summary>
    public static LogEvent StoringIntoOutbox { get; } = new(
        LogLevel.Debug,
        GetEventId(71, nameof(StoringIntoOutbox)),
        "Storing message into the transactional outbox. | endpointName: {endpointName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="IOutboxWorker" /> loads a batch of enqueued messages.
    /// </summary>
    public static LogEvent ReadingMessagesFromOutbox { get; } = new(
        LogLevel.Trace,
        GetEventId(72, nameof(ReadingMessagesFromOutbox)),
        "Reading batch of {readBatchSize} messages from the outbox queue...");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="IOutboxWorker" /> doesn't find any message in the outbox.
    /// </summary>
    public static LogEvent OutboxEmpty { get; } = new(
        LogLevel.Trace,
        GetEventId(73, nameof(OutboxEmpty)),
        "The outbox is empty.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message stored in the outbox is being processed.
    /// </summary>
    public static LogEvent ProcessingOutboxStoredMessage { get; } = new(
        LogLevel.Debug,
        GetEventId(74, nameof(ProcessingOutboxStoredMessage)),
        "Processing outbox message {currentMessageIndex}.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs  producing the message stored in the
    ///     outbox.
    /// </summary>
    public static LogEvent ErrorProducingOutboxStoredMessage { get; } = new(
        LogLevel.Error,
        GetEventId(75, nameof(ErrorProducingOutboxStoredMessage)),
        "Failed to produce the message stored in the outbox. | endpointName: {endpointName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while the <see cref="IOutboxWorker" />
    ///     processes the outbox queue.
    /// </summary>
    public static LogEvent ErrorProcessingOutbox { get; } = new(
        LogLevel.Error,
        GetEventId(76, nameof(ErrorProcessingOutbox)),
        "Error occurred processing the outbox.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an invalid message
    ///     has been produced (see <see cref="MessageValidationMode" />).
    /// </summary>
    public static LogEvent InvalidMessageProduced { get; } = new(
        LogLevel.Warning,
        GetEventId(81, nameof(InvalidMessageProduced)),
        "Invalid message produced: {validationErrors} | endpointName: {endpointName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an invalid message
    ///     has been consumed (see <see cref="MessageValidationMode" />).
    /// </summary>
    public static LogEvent InvalidMessageConsumed { get; } = new(
        LogLevel.Warning,
        GetEventId(82, nameof(InvalidMessageConsumed)),
        "Invalid message consumed: {validationErrors} | endpointName: {endpointName}, messageId: {messageId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when trying to connect an endpoint
    ///     with an invalid configuration.
    /// </summary>
    public static LogEvent InvalidEndpointConfiguration { get; } = new(
        LogLevel.Critical,
        GetEventId(101, nameof(InvalidEndpointConfiguration)),
        "Invalid configuration for endpoint '{endpointName}'.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown from
    ///     within an <see cref="IBrokerClientsConfigurator" />.
    /// </summary>
    public static LogEvent EndpointConfiguratorError { get; } = new(
        LogLevel.Critical,
        GetEventId(102, nameof(EndpointConfiguratorError)),
        "Error occurred configuring the endpoints. | configurator: {endpointsConfiguratorName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
    ///     by a client callback handler.
    /// </summary>
    public static LogEvent CallbackError { get; } = new(
        LogLevel.Error,
        GetEventId(103, nameof(CallbackError)),
        "Error occurred invoking the callback handler(s).");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown by
    ///     the endpoint builder action.
    /// </summary>
    public static LogEvent EndpointBuilderError { get; } = new(
        LogLevel.Critical,
        GetEventId(104, nameof(InvalidEndpointConfiguration)),
        "Failed to configure endpoint '{endpointName}'.");

    /// <summary>
    ///     Gets the <see cref="EventId" /> of the unspecific tracing logs.
    /// </summary>
    public static LogEvent Tracing { get; } = new(
        LogLevel.Trace,
        GetEventId(999, nameof(Tracing)),
        "The actual message will vary.");

    private static EventId GetEventId(int id, string name) =>
        new(1000 + id, $"Silverback.Integration_{name}");
}
