// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when an error is reported by the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />
    ///     (e.g. connection failures or all brokers down).
    ///     Note that the system (either the Kafka client itself or Silverback) will try to automatically recover from
    ///     all errors automatically, so these errors have to be considered purely informational.
    /// </summary>
    public class KafkaErrorEvent : IKafkaEvent
    {
        public KafkaErrorEvent(Error error)
        {
            Error = error;
        }

        /// <summary>
        ///     Gets an <see cref="Confluent.Kafka.Error" /> representing the error that occured when
        ///     interacting with the Kafka broker or the librdkafka library.
        /// </summary>
        public Error Error { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether this error has been handled in the subscriber and doesn't need to be logged or
        ///     handled in any other way by Silverback.
        /// </summary>
        public bool Handled { get; set; }
    }
}