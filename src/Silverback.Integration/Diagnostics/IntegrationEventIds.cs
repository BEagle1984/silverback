// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> constants of all events logged by the Silverback.Integration
    ///     package.
    /// </summary>
    public static class IntegrationEventIds
    {
        private const string Prefix = "Silverback.Integration_";

        private const int Offset = 1000;

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an inbound message is being processed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId ProcessingInboundMessage { get; } =
            new(Offset + 1, Prefix + nameof(ProcessingInboundMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while processing an
        ///     inbound message.
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ErrorProcessingInboundMessage { get; } =
            new(Offset + 2, Prefix + nameof(ErrorProcessingInboundMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an inbound message is added to a
        ///     sequence (e.g. a <see cref="ChunkSequence" /> or a <see cref="BatchSequence" />).
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId MessageAddedToSequence { get; } =
            new(Offset + 3, Prefix + nameof(MessageAddedToSequence));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the first message of a new sequence is
        ///     consumed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId SequenceStarted { get; } =
            new(Offset + 4, Prefix + nameof(SequenceStarted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when all messages belonging to the sequence
        ///     have been consumed and published to the internal bus.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId SequenceCompleted { get; } =
            new(Offset + 5, Prefix + nameof(SequenceCompleted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the processing of a sequence of
        ///     messages is aborted, but not because of an error (an <see cref="ErrorProcessingInboundMessage" /> is
        ///     logged instead) or an incomplete sequence that gets discarded (an
        ///     <see cref="IncompleteSequenceDiscarded" /> is logged instead).
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId SequenceProcessingAborted { get; } =
            new(Offset + 6, Prefix + nameof(SequenceProcessingAborted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an incomplete sequence is discarded
        ///     (e.g. the consumer started in the middle of sequence or a new sequence starts before the current one
        ///     is
        ///     completed or the timeout elapses before the sequence can be completed).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId IncompleteSequenceDiscarded { get; } =
            new(Offset + 7, Prefix + nameof(IncompleteSequenceDiscarded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when connecting to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId BrokerConnecting { get; } =
            new(Offset + 11, Prefix + nameof(BrokerConnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when connected to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BrokerConnected { get; } =
            new(Offset + 12, Prefix + nameof(BrokerConnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when disconnecting from the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId BrokerDisconnecting { get; } =
            new(Offset + 13, Prefix + nameof(BrokerDisconnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when disconnected from the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BrokerDisconnected { get; } =
            new(Offset + 14, Prefix + nameof(BrokerDisconnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a new consumer is being instantiated.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId CreatingNewConsumer { get; } =
            new(Offset + 15, Prefix + nameof(CreatingNewConsumer));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a new producer is being instantiated.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId CreatingNewProducer { get; } =
            new(Offset + 16, Prefix + nameof(CreatingNewProducer));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an exception is thrown when connecting
        ///     to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId BrokerConnectionError { get; } =
            new(Offset + 17, Prefix + nameof(BrokerConnectionError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the consumer is connected to the
        ///     endpoint and will start consuming.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumerConnected { get; } =
            new(Offset + 21, Prefix + nameof(ConsumerConnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the consumer is disconnected from the
        ///     endpoint.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumerDisconnected { get; } =
            new(Offset + 22, Prefix + nameof(ConsumerDisconnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an unhandled error occurs while
        ///     consuming and the consumer will be stopped.
        /// </summary>
        /// <remarks>
        ///     Default log level: Critical.
        /// </remarks>
        public static EventId ConsumerFatalError { get; } =
            new(Offset + 23, Prefix + nameof(ConsumerFatalError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the consumer is
        ///     disconnecting. This may in some cases cause inconsistencies, with some messages not being properly
        ///     committed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ConsumerDisposingError { get; } =
            new(Offset + 24, Prefix + nameof(ConsumerDisposingError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs in the consumer during
        ///     the commit operation.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ConsumerCommitError { get; } =
            new(Offset + 25, Prefix + nameof(ConsumerCommitError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs in the consumer during
        ///     the rollback operation.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ConsumerRollbackError { get; } =
            new(Offset + 26, Prefix + nameof(ConsumerRollbackError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message is produced.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageProduced { get; } =
            new(Offset + 31, Prefix + nameof(MessageProduced));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the maximum configured failed attempts for the policies are exceeded and the policy isn't applied
        ///     anymore.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyMaxFailedAttemptsExceeded { get; } =
            new(Offset + 41, Prefix + nameof(PolicyMaxFailedAttemptsExceeded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because the current exception type is not in the inclusion list.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyExceptionNotIncluded { get; } =
            new(Offset + 42, Prefix + nameof(PolicyExceptionNotIncluded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because the current exception type is in the exclusion list.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyExceptionExcluded { get; } =
            new(Offset + 43, Prefix + nameof(PolicyExceptionExcluded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because of it's apply rule.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyApplyRuleReturnedFalse { get; } =
            new(Offset + 44, Prefix + nameof(PolicyApplyRuleReturnedFalse));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when all chained policies have been applied
        ///     but the processing still failed and the consumer will therefore be stopped.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId PolicyChainCompleted { get; } =
            new(Offset + 45, Prefix + nameof(PolicyChainCompleted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and
        ///     the retry policy is applied, processing the message again.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId RetryMessageProcessing { get; } =
            new(Offset + 46, Prefix + nameof(RetryMessageProcessing));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and a
        ///     delay is applied before retrying it. This event occurs when the <c>MoveMessageErrorPolicy</c> that is
        ///     applied with a delay.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId RetryDelayed { get; } =
            new(Offset + 47, Prefix + nameof(RetryDelayed));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and is
        ///     moved to another endpoint. This event occurs when the <c>MoveMessageErrorPolicy</c> is applied.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageMoved { get; } =
            new(Offset + 48, Prefix + nameof(MessageMoved));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and is
        ///     skipped. This event occurs when the <c>SkipMessageErrorPolicy</c> is applied.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageSkipped { get; } =
            new(Offset + 49, Prefix + nameof(MessageSkipped));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the <c>MoveMessageErrorPolicy</c>
        ///     cannot be applied because the failing message belongs to a sequences (it's either chunked, being
        ///     processed in batch, etc.).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId CannotMoveSequences { get; } =
            new(Offset + 50, Prefix + nameof(MessageMoved));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while trying to
        ///     initialize the <see cref="Activity" /> from the message headers (distributed tracing).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ErrorInitializingActivity { get; } =
            new(Offset + 61, Prefix + nameof(ErrorInitializingActivity));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message is being skipped since
        ///     already processed (according to the stored offsets or message id).
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageAlreadyProcessed { get; } =
            new(Offset + 72, Prefix + nameof(MessageAlreadyProcessed));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message is being written to the
        ///     outbox.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId OutboundMessageWrittenToOutbox { get; } =
            new(Offset + 73, Prefix + nameof(OutboundMessageWrittenToOutbox));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IOutboxWorker" /> loads a batch of enqueued messages.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId ReadingMessagesFromOutbox { get; } =
            new(Offset + 74, Prefix + nameof(ReadingMessagesFromOutbox));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IOutboxWorker" /> doesn't find any message in the outbox.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId OutboxEmpty { get; } =
            new(Offset + 75, Prefix + nameof(OutboxEmpty));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message stored in the outbox is
        ///     being processed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ProcessingOutboxStoredMessage { get; } =
            new(Offset + 76, Prefix + nameof(ProcessingOutboxStoredMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while producing the
        ///     message stored in the outbox.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ErrorProducingOutboxStoredMessage { get; } =
            new(Offset + 77, Prefix + nameof(ErrorProducingOutboxStoredMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IOutboxWorker" /> processes the outbound queue.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ErrorProcessingOutboundQueue { get; } =
            new(Offset + 78, Prefix + nameof(ErrorProcessingOutboundQueue));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the low level tracing logs.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId LowLevelTracing { get; } =
            new(Offset + 999, Prefix + nameof(LowLevelTracing));
    }
}
