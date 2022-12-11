// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnProducerLog" /> event handler.
/// </summary>
public interface IKafkaProducerLogCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when a log message is being reported by the underlying producer.
    /// </summary>
    /// <param name="logMessage">
    ///     The <see cref="LogMessage" />.
    /// </param>
    /// <param name="producerWrapper">
    ///     The related <see cref="IConfluentProducerWrapper" />.
    /// </param>
    /// <returns>
    ///     A value whether the log message was handled/written. When <c>true</c> the message will not be logged nor
    ///     handled in any other way by Silverback.
    /// </returns>
    bool OnProducerLog(LogMessage logMessage, IConfluentProducerWrapper producerWrapper);
}
