// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="LogEvent" /> constants of all events logged by the Silverback.Integration
    ///     package.
    /// </summary>
    [SuppressMessage("", "SA1118", Justification = "Cleaner and clearer this way")]
    public static class IntegrationLogEvents
    {
        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an inbound message is being
        ///     processed.
        /// </summary>
        public static LogEvent ProcessingInboundMessage { get; } = new(
            LogLevel.Information,
            GetEventId(1, nameof(ProcessingInboundMessage)),
            "Processing inbound message.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while
        ///     processing an inbound message.
        /// </summary>
        /// <remarks>
        ///     If the message belongs to a sequence the <see cref="ErrorProcessingInboundSequence" /> event is logged
        ///     instead.
        /// </remarks>
        public static LogEvent ErrorProcessingInboundMessage { get; } = new(
            LogLevel.Warning,
            GetEventId(2, nameof(ErrorProcessingInboundMessage)),
            "Error occurred processing the inbound message.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an inbound message is added to
        ///     a sequence (e.g. <see cref="ChunkSequence" /> or a <see cref="BatchSequence" />).
        /// </summary>
        public static LogEvent MessageAddedToSequence { get; } = new(
            LogLevel.Debug,
            GetEventId(3, nameof(MessageAddedToSequence)),
            "Message '{messageId}' added to {sequenceType} '{sequenceId}'. | length: {sequenceLength}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the first message of a new
        ///     sequence is consumed.
        /// </summary>
        public static LogEvent SequenceStarted { get; } = new(
            LogLevel.Debug,
            GetEventId(4, nameof(SequenceStarted)),
            "Started new {sequenceType} '{sequenceId}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when all messages belonging to the
        ///     sequence have been consumed and published to the internal bus.
        /// </summary>
        public static LogEvent SequenceCompleted { get; } = new(
            LogLevel.Debug,
            GetEventId(5, nameof(SequenceCompleted)),
            "{sequenceType} '{sequenceId}' completed. | length: {sequenceLength}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of a sequence
        ///     of messages is aborted, but not because of an error (an <see cref="ErrorProcessingInboundSequence" /> is
        ///     logged instead) or an incomplete sequence that gets discarded (an
        ///     <see cref="IncompleteSequenceAborted" /> or an <see cref="SkippingIncompleteSequence" /> is logged
        ///     instead).
        /// </summary>
        public static LogEvent SequenceProcessingAborted { get; } = new(
            LogLevel.Debug,
            GetEventId(6, nameof(SequenceProcessingAborted)),
            "The {sequenceType} '{sequenceId}' processing has been aborted. | " +
            "length: {sequenceLength}, reason: {reason}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while
        ///     processing an inbound sequence.
        /// </summary>
        public static LogEvent ErrorProcessingInboundSequence { get; } = new(
            LogLevel.Debug,
            GetEventId(7, nameof(ErrorProcessingInboundSequence)),
            "Error occurred processing the {sequenceType} '{sequenceId}'. | " +
            "length: {sequenceLength}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an incomplete sequence is
        ///     aborted because a new sequence starts before the current one is completed or the timeout elapses before
        ///     the sequence can be completed).
        /// </summary>
        public static LogEvent IncompleteSequenceAborted { get; } = new(
            LogLevel.Warning,
            GetEventId(8, nameof(IncompleteSequenceAborted)),
            "The incomplete {sequenceType} '{sequenceId}' is aborted.  | " +
            "length: {sequenceLength}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an incomplete sequence is
        ///     skipped because first consumed message of that sequence doesn't correspond to the actual sequence
        ///     beginning (e.g. the first chunk).
        /// </summary>
        public static LogEvent SkippingIncompleteSequence { get; } = new(
            LogLevel.Warning,
            GetEventId(9, nameof(SkippingIncompleteSequence)),
            "Skipping the incomplete sequence '{sequenceId}'. The first message is missing.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while
        ///     aborting an inbound sequence.
        /// </summary>
        public static LogEvent ErrorAbortingInboundSequence { get; } = new(
            LogLevel.Warning,
            GetEventId(110, nameof(ErrorAbortingInboundSequence)),
            "Error occurred aborting the {sequenceType} '{sequenceId}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when connecting to the message
        ///     broker.
        /// </summary>
        public static LogEvent BrokerConnecting { get; } = new(
            LogLevel.Debug,
            GetEventId(11, nameof(BrokerConnecting)),
            "{broker} connecting to message broker...");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when connected to the message
        ///     broker.
        /// </summary>
        public static LogEvent BrokerConnected { get; } = new(
            LogLevel.Information,
            GetEventId(12, nameof(BrokerConnected)),
            "{broker} connected to message broker.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnecting from the message
        ///     broker.
        /// </summary>
        public static LogEvent BrokerDisconnecting { get; } = new(
            LogLevel.Debug,
            GetEventId(13, nameof(BrokerDisconnecting)),
            "{broker} disconnecting from message broker...");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when disconnected from the message
        ///     broker.
        /// </summary>
        public static LogEvent BrokerDisconnected { get; } = new(
            LogLevel.Information,
            GetEventId(14, nameof(BrokerDisconnected)),
            "{broker} disconnected from message broker.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer is being
        ///     instantiated.
        /// </summary>
        public static LogEvent CreatingNewConsumer { get; } = new(
            LogLevel.Information,
            GetEventId(15, nameof(CreatingNewConsumer)),
            "Creating new consumer for endpoint '{endpointName}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new producer is being
        ///     instantiated.
        /// </summary>
        public static LogEvent CreatingNewProducer { get; } = new(
            LogLevel.Information,
            GetEventId(16, nameof(CreatingNewProducer)),
            "Creating new producer for endpoint '{endpointName}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown when
        ///     connecting to the message broker.
        /// </summary>
        public static LogEvent BrokerConnectionError { get; } = new(
            LogLevel.Error,
            GetEventId(17, nameof(BrokerConnectionError)),
            "Error occurred connecting to the message broker(s).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer is connected to
        ///     the endpoint and will start consuming.
        /// </summary>
        public static LogEvent ConsumerConnected { get; } = new(
            LogLevel.Debug,
            GetEventId(21, nameof(ConsumerConnected)),
            "Connected consumer to endpoint.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer is disconnected
        ///     from the endpoint.
        /// </summary>
        public static LogEvent ConsumerDisconnected { get; } = new(
            LogLevel.Debug,
            GetEventId(22, nameof(ConsumerDisconnected)),
            "Disconnected consumer from endpoint.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an unhandled error occurs
        ///     while consuming and the consumer will be stopped.
        /// </summary>
        public static LogEvent ConsumerFatalError { get; } = new(
            LogLevel.Critical,
            GetEventId(23, nameof(ConsumerFatalError)),
            "Fatal error occurred processing the consumed message. The consumer will be stopped.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while the
        ///     consumer is disconnecting. This may in some cases cause inconsistencies, with some messages not being
        ///     properly committed.
        /// </summary>
        public static LogEvent ConsumerDisposingError { get; } = new(
            LogLevel.Warning,
            GetEventId(24, nameof(ConsumerDisposingError)),
            "Error occurred while disposing the consumer.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
        ///     consumer during the commit operation.
        /// </summary>
        public static LogEvent ConsumerCommitError { get; } = new(
            LogLevel.Error,
            GetEventId(25, nameof(ConsumerCommitError)),
            "Commit failed.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs in the
        ///     consumer during the rollback operation.
        /// </summary>
        public static LogEvent ConsumerRollbackError { get; } = new(
            LogLevel.Error,
            GetEventId(26, nameof(ConsumerRollbackError)),
            "Rollback failed.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer is connected to
        ///     the endpoint and ready to produce.
        /// </summary>
        public static LogEvent ProducerConnected { get; } = new(
            LogLevel.Debug,
            GetEventId(27, nameof(ProducerConnected)),
            "Connected producer to endpoint.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer is disconnected
        ///     from the endpoint.
        /// </summary>
        public static LogEvent ProducerDisconnected { get; } = new(
            LogLevel.Debug,
            GetEventId(28, nameof(ProducerDisconnected)),
            "Disconnected producer from endpoint.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is produced.
        /// </summary>
        public static LogEvent MessageProduced { get; } = new(
            LogLevel.Information,
            GetEventId(31, nameof(MessageProduced)),
            "Message produced.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written during the evaluation of an error
        ///     policy, when the maximum configured failed attempts for the policies are exceeded and the policy isn't
        ///     applied anymore.
        /// </summary>
        public static LogEvent PolicyMaxFailedAttemptsExceeded { get; } = new(
            LogLevel.Trace,
            GetEventId(41, nameof(PolicyMaxFailedAttemptsExceeded)),
            "The {policyType} will be skipped because the current failed " +
            "attempts ({failedAttempts}) exceeds the configured maximum attempts " +
            "({maxFailedAttempts}).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written during the evaluation of an error
        ///     policy, when the policy isn't applied because the current exception type is not in the inclusion list.
        /// </summary>
        public static LogEvent PolicyExceptionNotIncluded { get; } = new(
            LogLevel.Trace,
            GetEventId(42, nameof(PolicyExceptionNotIncluded)),
            "The {policyType} will be skipped because the {exceptionType} is not in the list of handled exceptions.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written during the evaluation of an error
        ///     policy, when the policy isn't applied because the current exception type is in the exclusion list.
        /// </summary>
        public static LogEvent PolicyExceptionExcluded { get; } = new(
            LogLevel.Trace,
            GetEventId(43, nameof(PolicyExceptionExcluded)),
            "The {policyType} will be skipped because the {exceptionType} is in the list of excluded exceptions.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written during the evaluation of an error
        ///     policy, when the policy isn't applied because of it's apply rule.
        /// </summary>
        public static LogEvent PolicyApplyRuleReturnedFalse { get; } = new(
            LogLevel.Trace,
            GetEventId(44, nameof(PolicyApplyRuleReturnedFalse)),
            "The {policyType} will be skipped because the apply rule evaluated to false.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when all chained policies have been
        ///     applied but the processing still failed and the consumer will therefore be stopped.
        /// </summary>
        public static LogEvent PolicyChainCompleted { get; } = new(
            LogLevel.Trace,
            GetEventId(45, nameof(PolicyChainCompleted)),
            "All policies have been applied but the message(s) couldn't be successfully processed. " +
            "The consumer will be stopped.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
        ///     processed and the retry policy is applied, processing the message again. This event occurs when the
        ///     <see cref="RetryErrorPolicy" /> is applied.
        /// </summary>
        public static LogEvent RetryMessageProcessing { get; } = new(
            LogLevel.Information,
            GetEventId(46, nameof(RetryMessageProcessing)),
            "The message(s) will be processed again.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
        ///     processed and a delay is applied before retrying it.
        /// </summary>
        public static LogEvent RetryDelayed { get; } = new(
            LogLevel.Trace,
            GetEventId(47, nameof(RetryDelayed)),
            "Waiting {delay} milliseconds before retrying to process the message(s).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
        ///     processed and is moved to another endpoint. This event occurs when the
        ///     <see cref="MoveMessageErrorPolicy" /> is applied.
        /// </summary>
        public static LogEvent MessageMoved { get; } = new(
            LogLevel.Information,
            GetEventId(48, nameof(MessageMoved)),
            "The message will be moved to the endpoint '{targetEndpointName}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be
        ///     processed and is skipped. This event occurs when the <see cref="SkipMessageErrorPolicy" /> is applied.
        /// </summary>
        public static LogEvent MessageSkipped { get; } = new(
            LogLevel.Information,
            GetEventId(49, nameof(MessageSkipped)),
            "The message(s) will be skipped.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
        ///     <see cref="MoveMessageErrorPolicy" /> cannot be applied because the failing message belongs to a
        ///     sequences (it's either chunked, being processed in batch, etc.).
        /// </summary>
        public static LogEvent CannotMoveSequences { get; } = new(
            LogLevel.Warning,
            GetEventId(50, nameof(CannotMoveSequences)),
            "The message belongs to a {sequenceType} and cannot be moved.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while trying
        ///     to initialize the <see cref="Activity" /> from the message headers (distributed tracing).
        /// </summary>
        public static LogEvent ErrorInitializingActivity { get; } = new(
            LogLevel.Warning,
            GetEventId(61, nameof(ErrorInitializingActivity)),
            "Failed to initialize the current activity from the message headers.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is being skipped
        ///     since already processed (according to the stored offsets or message id).
        /// </summary>
        public static LogEvent MessageAlreadyProcessed { get; } = new(
            LogLevel.Information,
            GetEventId(72, nameof(MessageAlreadyProcessed)),
            "Message is being skipped since it was already processed.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is being written
        ///     to the outbox.
        /// </summary>
        public static LogEvent MessageWrittenToOutbox { get; } = new(
            LogLevel.Debug,
            GetEventId(73, nameof(MessageWrittenToOutbox)),
            "Writing the outbound message to the transactional outbox.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
        ///     <see cref="IOutboxWorker" /> loads a batch of enqueued messages.
        /// </summary>
        public static LogEvent ReadingMessagesFromOutbox { get; } = new(
            LogLevel.Trace,
            GetEventId(74, nameof(ReadingMessagesFromOutbox)),
            "Reading a batch of {readBatchSize} messages from the outbox queue...");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
        ///     <see cref="IOutboxWorker" /> doesn't find any message in the outbox.
        /// </summary>
        public static LogEvent OutboxEmpty { get; } = new(
            LogLevel.Trace,
            GetEventId(75, nameof(OutboxEmpty)),
            "The outbox is empty.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message stored in the
        ///     outbox is being processed.
        /// </summary>
        public static LogEvent ProcessingOutboxStoredMessage { get; } = new(
            LogLevel.Debug,
            GetEventId(76, nameof(ProcessingOutboxStoredMessage)),
            "Processing outbox message {currentMessageIndex} of {totalMessages}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while
        ///     producing the message stored in the outbox.
        /// </summary>
        public static LogEvent ErrorProducingOutboxStoredMessage { get; } = new(
            LogLevel.Error,
            GetEventId(77, nameof(ErrorProducingOutboxStoredMessage)),
            "Failed to produce the message stored in the outbox.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while the
        ///     <see cref="IOutboxWorker" /> processes the outbox queue.
        /// </summary>
        public static LogEvent ErrorProcessingOutbox { get; } = new(
            LogLevel.Error,
            GetEventId(78, nameof(ErrorProcessingOutbox)),
            "Error occurred processing the outbox.");

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the low level tracing logs.
        /// </summary>
        public static LogEvent LowLevelTracing { get; } = new(
            LogLevel.Trace,
            GetEventId(999, nameof(LowLevelTracing)),
            "The actual message will vary.");

        private static EventId GetEventId(int id, string name) =>
            new(1000 + id, $"Silverback.Integration_{name}");
    }
}
