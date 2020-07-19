// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> constants of all events logged by the Silverback.Integration.Kafka
    ///     package.
    /// </summary>
    public static class RabbitEventIds
    {
        private const string Prefix = "Silverback.Integration.Kafka_";

        private const int Offset = 3000;

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a message is consumed from a Rabbit
        ///     queue.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId ConsumingMessage { get; } =
            new EventId(Offset + 11, Prefix + nameof(ConsumingMessage));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a delivery tag is successfully
        ///     committed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId Commit { get; } =
            new EventId(Offset + 12, Prefix + nameof(Commit));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while committing the
        ///     delivery tag.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId CommitError { get; } =
            new EventId(Offset + 13, Prefix + nameof(CommitError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when a delivery tag is successfully rolled
        ///     back.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId Rollback { get; } =
            new EventId(Offset + 14, Prefix + nameof(Rollback));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while rolling back the
        ///     delivery tag.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId RollbackError { get; } =
            new EventId(Offset + 15, Prefix + nameof(RollbackError));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the processing of the producer queue
        ///     is being stopped (usually
        ///     because the broker is being disconnected or the application is exiting).
        /// </summary>
        /// <remarks>
        ///     Default log level: Trace.
        /// </remarks>
        public static EventId ProducerQueueProcessingCanceled { get; } =
            new EventId(Offset + 21, Prefix + nameof(ProducerQueueProcessingCanceled));
    }
}
