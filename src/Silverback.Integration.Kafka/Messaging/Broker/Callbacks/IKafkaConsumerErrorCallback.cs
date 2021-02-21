// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks
{
    /// <summary>
    ///     Declares the <see cref="OnConsumerError" /> event handler.
    /// </summary>
    public interface IKafkaConsumerErrorCallback : IBrokerCallback
    {
        /// <summary>
        ///     Called when an error is reported by the underlying consumer.
        /// </summary>
        /// <remarks>
        ///     Note that the system (either the Kafka client itself or Silverback) will try to automatically
        ///     recover from all errors automatically, so these errors have to be considered mostly informational.
        /// </remarks>
        /// <param name="error">
        ///     An <see cref="Error" /> containing the error details.
        /// </param>
        /// <param name="consumer">
        ///     The related consumer instance.
        /// </param>
        /// <returns>
        ///     A value whether the error was handled. When <c>true</c> the error will not be logged nor handled in
        ///     any other way by Silverback.
        /// </returns>
        bool OnConsumerError(Error error, KafkaConsumer consumer);
    }
}
