// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> constants of all events logged by the Silverback.Integration.Kafka
    ///     package.
    /// </summary>
    public static class KafkaEventIds
    {
        private const string Prefix = "Silverback.Integration.Kafka_";

        private const int Offset = 2000;

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message is consumed from a Kafka
        ///     topic.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumingMessage { get; } =
            new EventId(Offset + 11, Prefix + nameof(ConsumingMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the end of partition is reached.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId EndOfPartition { get; } =
            new EventId(Offset + 12, Prefix + nameof(EndOfPartition));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a
        ///     <see cref="KafkaExceptionAutoRecovery" /> is thrown inside the <c>Consume</c> method. The consumer
        ///     will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId KafkaExceptionAutoRecovery { get; } =
            new EventId(Offset + 13, Prefix + nameof(KafkaExceptionAutoRecovery));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a
        ///     <see cref="KafkaExceptionAutoRecovery" /> is thrown inside the <c>Consume</c> method. The consumer
        ///     will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
        /// </summary>
        /// <remarks>
        ///     Default log level: Critical.
        /// </remarks>
        public static EventId KafkaExceptionNoAutoRecovery { get; } =
            new EventId(Offset + 14, Prefix + nameof(KafkaExceptionNoAutoRecovery));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the <see cref="KafkaConsumer" /> is
        ///     unable to recover from the <see cref="KafkaExceptionAutoRecovery" />.
        /// </summary>
        /// <remarks>
        ///     Default log level: Critical.
        /// </remarks>
        public static EventId ErrorRecoveringFromKafkaException { get; } =
            new EventId(Offset + 15, Prefix + nameof(ErrorRecoveringFromKafkaException));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the <c>Consume</c> is aborted (usually
        ///     because the broker is being disconnected or the application is exiting).
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId ConsumingCanceled { get; } =
            new EventId(Offset + 16, Prefix + nameof(ConsumingCanceled));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="Confluent.Kafka.Producer{TKey,TValue}" /> is being instantiated.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId CreatingConfluentProducer { get; } =
            new EventId(Offset + 21, Prefix + nameof(CreatingConfluentProducer));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the message is sent to the broker but
        ///     no acknowledge is received. This is logged only if <c>ThrowIfNotAcknowledged</c> is <c>false</c>.
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ProduceNotAcknowledged { get; } =
            new EventId(Offset + 22, Prefix + nameof(ProduceNotAcknowledged));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a new consumer group partition
        ///     assignment has been received by a consumer.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId PartitionsAssigned { get; } =
            new EventId(Offset + 31, Prefix + nameof(PartitionsAssigned));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the offset is being reset.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId PartitionOffsetReset { get; } =
            new EventId(Offset + 32, Prefix + nameof(PartitionOffsetReset));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a group partition assignment is being
        ///     revoked.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId PartitionsRevoked { get; } =
            new EventId(Offset + 33, Prefix + nameof(PartitionsRevoked));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an offset is successfully committed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId OffsetCommitted { get; } =
            new EventId(Offset + 34, Prefix + nameof(OffsetCommitted));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while committing the
        ///     offset.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId KafkaEventsHandlerErrorWhileCommittingOffset { get; } =
            new EventId(Offset + 35, Prefix + nameof(KafkaEventsHandlerErrorWhileCommittingOffset));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error is reported by the
        ///     <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error or Critical (according to the <c>IsFatal</c> property value of the reported
        ///     error).
        /// </remarks>
        public static EventId ConsumerError { get; } =
            new EventId(Offset + 36, Prefix + nameof(ConsumerError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a subscriber to the
        ///     <see cref="KafkaErrorEvent" /> throws an unhandled exception.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId KafkaErrorHandlerError { get; } =
            new EventId(Offset + 37, Prefix + nameof(KafkaErrorHandlerError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the consumer statistics are received.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumerStatisticsReceived { get; } =
            new EventId(Offset + 38, Prefix + nameof(ConsumerStatisticsReceived));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the producer statistics are received.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ProducerStatisticsReceived { get; } =
            new EventId(Offset + 39, Prefix + nameof(ProducerStatisticsReceived));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an exception is thrown disconnecting
        ///     the consumer.
        /// </summary>
        /// <remarks>
        ///     Default log level: Warning.
        /// </remarks>
        public static EventId ConsumerDisconnectError { get; } =
            new EventId(Offset + 50, Prefix + nameof(ConsumerDisconnectError));
    }
}
