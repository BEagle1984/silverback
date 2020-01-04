// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired when an error is reported by the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />
    ///     (e.g. connection failures or all brokers down).
    ///     Note that the system (either the Kafka client itself or Silverback) will try to automatically recover from
    ///     all errors automatically, so these errors have to be considered purely informational.
    /// </summary>
    public class KafkaErrorEvent : IKafkaEvent
    {
        public KafkaErrorEvent(Confluent.Kafka.Error error)
        {
            Error = error;
        }

        /// <summary>
        ///     Gets an <see cref="Confluent.Kafka.Error" /> instance representing the error that occured when
        ///     interacting with the Kafka broker or the librdkafka library.
        /// </summary>
        public Confluent.Kafka.Error Error { get; }
    }
}