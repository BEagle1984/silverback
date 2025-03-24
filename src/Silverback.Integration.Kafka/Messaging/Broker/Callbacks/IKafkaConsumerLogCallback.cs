// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnConsumerLog" /> event handler.
/// </summary>
public interface IKafkaConsumerLogCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when a log message is being reported by the underlying consumer.
    /// </summary>
    /// <param name="logMessage">
    ///     The <see cref="LogMessage" />.
    /// </param>
    /// <param name="consumer">
    ///     The related consumer instance.
    /// </param>
    /// <returns>
    ///     A value whether the log message was handled/written. When <c>true</c> the message will not be logged nor
    ///     handled in any other way by Silverback.
    /// </returns>
    bool OnConsumerLog(LogMessage logMessage, IKafkaConsumer consumer);
}
