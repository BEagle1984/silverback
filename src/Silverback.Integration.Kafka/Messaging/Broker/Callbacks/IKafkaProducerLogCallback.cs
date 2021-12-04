// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnProducerLog" /> event handler.
/// </summary>
public interface IKafkaProducerLogCallback : IBrokerCallback
{
    /// <summary>
    ///     Called when a log message is being reported by the underlying producer.
    /// </summary>
    /// <param name="logMessage">
    ///     The <see cref="LogMessage" />.
    /// </param>
    /// <param name="producer">
    ///     The related producer instance.
    /// </param>
    /// <returns>
    ///     A value whether the log message was handled/written. When <c>true</c> the message will not be logged nor
    ///     handled in any other way by Silverback.
    /// </returns>
    bool OnProducerLog(LogMessage logMessage, KafkaProducer producer);
}
