// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="LogEvent" /> constants of all events logged by the
    ///     Silverback.Integration.RabbitMQ package.
    /// </summary>
    public static class RabbitLogEvents
    {
        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a
        ///     Rabbit queue.
        /// </summary>
        public static LogEvent ConsumingMessage { get; } = new(
            LogLevel.Debug,
            GetEventId(11, nameof(ConsumingMessage)),
            "Consuming message {deliveryTag}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a delivery tag is successfully
        ///     committed.
        /// </summary>
        public static LogEvent Commit { get; } = new(
            LogLevel.Debug,
            GetEventId(12, nameof(Commit)),
            "Successfully committed (basic.ack) the delivery tag {deliveryTag}.");

        /// <summary>
        ///     Reserved, not used anymore.
        /// </summary>
        [SuppressMessage("", "SA1623", Justification = "Reserved id")]
        [Obsolete("Logged in the base consumer.", true)]
        public static LogEvent CommitError { get; } = new(
            LogLevel.Error,
            GetEventId(13, nameof(CommitError)),
            "Not used anymore.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a delivery tag is successfully
        ///     rolled back.
        /// </summary>
        public static LogEvent Rollback { get; } = new(
            LogLevel.Debug,
            GetEventId(14, nameof(Rollback)),
            "Successfully rolled back (basic.nack) the delivery tag {deliveryTag}.");

        /// <summary>
        ///     Reserved, not used anymore.
        /// </summary>
        [SuppressMessage("", "SA1623", Justification = "Reserved id")]
        [Obsolete("Logged in the base consumer.", true)]
        public static LogEvent RollbackError { get; } = new(
            LogLevel.Error,
            GetEventId(15, nameof(RollbackError)),
            "Not used anymore.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of the producer
        ///     queue is being stopped (usually because the application is exiting).
        /// </summary>
        public static LogEvent ProducerQueueProcessingCanceled { get; } = new(
                LogLevel.Debug,
                GetEventId(21, nameof(ProducerQueueProcessingCanceled)),
                "Producer queue processing was canceled.");

        private static EventId GetEventId(int id, string name) =>
            new(3000 + id, $"Silverback.Integration.RabbitMQ_{name}");
    }
}
