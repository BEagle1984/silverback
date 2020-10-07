// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> constants of all events logged by the Silverback.Integration
    ///     package.
    /// </summary>
    public static class IntegrationEventIds
    {
        // TODO: Review and remove unused


        private const string Prefix = "Silverback.Integration_";

        private const int Offset = 1000;

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an inbound message (or a batch of
        ///     messages) is being processed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId ProcessingInboundMessage { get; } =
            new EventId(Offset + 1, Prefix + nameof(ProcessingInboundMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while processing an
        ///     inbound message (or a batch of messages).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ErrorProcessingInboundMessage { get; } =
            new EventId(Offset + 2, Prefix + nameof(ErrorProcessingInboundMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an inbound message is added to a
        ///     batch.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageAddedToBatch { get; } =
            new EventId(Offset + 3, Prefix + nameof(MessageAddedToBatch));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when connecting to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId BrokerConnecting { get; } =
            new EventId(Offset + 11, Prefix + nameof(BrokerConnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when connected to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BrokerConnected { get; } =
            new EventId(Offset + 12, Prefix + nameof(BrokerConnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when disconnecting from the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId BrokerDisconnecting { get; } =
            new EventId(Offset + 13, Prefix + nameof(BrokerDisconnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when disconnected from the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BrokerDisconnected { get; } =
            new EventId(Offset + 14, Prefix + nameof(BrokerDisconnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a new consumer is being instantiated.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId CreatingNewConsumer { get; } =
            new EventId(Offset + 15, Prefix + nameof(CreatingNewConsumer));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a new producer is being instantiated.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId CreatingNewProducer { get; } =
            new EventId(Offset + 16, Prefix + nameof(CreatingNewProducer));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an exception is thrown when connecting
        ///     to the message broker.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId BrokerConnectionError { get; } =
            new EventId(Offset + 17, Prefix + nameof(BrokerConnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the consumer is connected to the
        ///     endpoint and will start consuming.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumerConnected { get; } =
            new EventId(Offset + 21, Prefix + nameof(ConsumerConnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the consumer is disconnected from the
        ///     endpoint.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumerDisconnected { get; } =
            new EventId(Offset + 22, Prefix + nameof(ConsumerDisconnected));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an unhandled error occurs while
        ///     consuming a message and the consumer will be stopped.
        /// </summary>
        /// <remarks>
        ///     Default log level: Critical.
        /// </remarks>
        public static EventId ConsumerFatalError { get; } =
            new EventId(Offset + 23, Prefix + nameof(ConsumerFatalError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the consumer is
        ///     disconnecting. This may in some cases cause inconsistencies, with some messages not being properly
        ///     committed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ConsumerDisposingError { get; } =
            new EventId(Offset + 24, Prefix + nameof(ConsumerDisposingError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message is produced.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageProduced { get; } =
            new EventId(Offset + 31, Prefix + nameof(MessageProduced));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the maximum configured failed attempts for the policies are exceeded and the policy isn't applied
        ///     anymore.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyMaxFailedAttemptsExceeded { get; } =
            new EventId(Offset + 41, Prefix + nameof(PolicyMaxFailedAttemptsExceeded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because the current exception type is not in the inclusion list.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyExceptionNotIncluded { get; } =
            new EventId(Offset + 42, Prefix + nameof(PolicyExceptionNotIncluded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because the current exception type is in the exclusion list.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyExceptionExcluded { get; } =
            new EventId(Offset + 43, Prefix + nameof(PolicyExceptionExcluded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written during the evaluation of an error policy,
        ///     when the policy isn't applied because of it's apply rule.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId PolicyApplyRuleReturnedFalse { get; } =
            new EventId(Offset + 44, Prefix + nameof(PolicyApplyRuleReturnedFalse));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when all chained policies have been applied
        ///     but the processing still failed and the consumer will therefore be stopped.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId PolicyChainCompleted { get; } =
            new EventId(Offset + 45, Prefix + nameof(PolicyChainCompleted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and
        ///     the retry policy is applied, processing the message again.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId RetryMessageProcessing { get; } =
            new EventId(Offset + 46, Prefix + nameof(RetryMessageProcessing));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and a
        ///     delay is applied before retrying it. This event occurs when the <c>MoveMessageErrorPolicy</c> that is
        ///     applied with a delay.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId RetryDelayed { get; } =
            new EventId(Offset + 47, Prefix + nameof(RetryDelayed));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and is
        ///     moved to another endpoint. This event occurs when the <c>MoveMessageErrorPolicy</c> is applied.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageMoved { get; } =
            new EventId(Offset + 48, Prefix + nameof(MessageMoved));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message couldn't be processed and is
        ///     skipped. This event occurs when the <c>SkipMessageErrorPolicy</c> is applied.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageSkipped { get; } =
            new EventId(Offset + 49, Prefix + nameof(MessageSkipped));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while trying to
        ///     initialize the <see cref="Activity" /> from the message headers (distributed tracing).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ErrorInitializingActivity { get; } =
            new EventId(Offset + 61, Prefix + nameof(ErrorInitializingActivity));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the inbound connector is creating the
        ///     consumer to connect to the inbound endpoint.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId InboundConnectorConnecting { get; } =
            new EventId(Offset + 71, Prefix + nameof(InboundConnectorConnecting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message is being skipped since
        ///     already processed (according to the stored offsets or message id).
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId MessageAlreadyProcessed { get; } =
            new EventId(Offset + 72, Prefix + nameof(MessageAlreadyProcessed));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message is being written to the
        ///     outbox.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId OutboundMessageWrittenToOutbox { get; } =
            new EventId(Offset + 73, Prefix + nameof(OutboundMessageWrittenToOutbox));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IOutboxWorker" /> loads a batch of enqueued messages.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId ReadingMessagesFromOutbox { get; } =
            new EventId(Offset + 74, Prefix + nameof(ReadingMessagesFromOutbox));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IOutboxWorker" /> doesn't find any message in the outbox.
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId OutboxEmpty { get; } =
            new EventId(Offset + 75, Prefix + nameof(OutboxEmpty));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the enqueued outbound message is being
        ///     processed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ProcessingEnqueuedOutboundMessage { get; } =
            new EventId(Offset + 76, Prefix + nameof(ProcessingEnqueuedOutboundMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while producing the
        ///     enqueued outbound message.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ErrorProducingEnqueuedMessage { get; } =
            new EventId(Offset + 77, Prefix + nameof(ErrorProducingEnqueuedMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IOutboxWorker" /> processes the outbound queue.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ErrorProcessingOutboundQueue { get; } =
            new EventId(Offset + 78, Prefix + nameof(ErrorProcessingOutboundQueue));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while cleaning up the
        ///     temporary chunks from the <see cref="IChunkStore" />.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId ErrorCleaningChunkStore { get; } =
            new EventId(Offset + 91, Prefix + nameof(ErrorCleaningChunkStore));
    }
}
