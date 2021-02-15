// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="LogEvent" /> constants of all events logged by the
    ///     Silverback.Integration.Mqtt package.
    /// </summary>
    [SuppressMessage("", "SA1118", Justification = "Cleaner and clearer this way")]
    public static class MqttLogEvents
    {
        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a
        ///     MQTT topic.
        /// </summary>
        public static LogEvent ConsumingMessage { get; } = new(
            LogLevel.Debug,
            GetEventId(11, nameof(ConsumingMessage)),
            "Consuming message '{messageId}' from topic '{topic}'.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while connecting to the MQTT broker.
        /// </summary>
        public static LogEvent ConnectError { get; } = new(
            LogLevel.Warning,
            GetEventId(21, nameof(ConnectError)),
            "Error occurred connecting to the MQTT broker. | clientId: {clientId}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while retrying to connect to the MQTT broker.
        /// </summary>
        public static LogEvent ConnectRetryError { get; } = new(
            LogLevel.Debug,
            GetEventId(22, nameof(ConnectRetryError)),
            "Error occurred retrying to connect to the MQTT broker. | clientId: {clientId}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the connection to the MQTT broker is lost.
        /// </summary>
        public static LogEvent ConnectionLost { get; } = new(
            LogLevel.Warning,
            GetEventId(23, nameof(ConnectionLost)),
            "Connection with the MQTT broker lost. The client will try to reconnect. | clientId: {clientId}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of the producer
        ///     queue is being stopped (usually because the application is exiting).
        /// </summary>
        public static LogEvent ProducerQueueProcessingCanceled { get; } = new(
            LogLevel.Debug,
            GetEventId(31, nameof(ProducerQueueProcessingCanceled)),
            "Producer queue processing was canceled.");

        private static EventId GetEventId(int id, string name) =>
            new(4000 + id, $"Silverback.Integration.Kafka_{name}");
    }
}
