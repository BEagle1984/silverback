// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
    ///     <see cref="KafkaProducerConfig" /> and <see cref="KafkaConsumerConfig" />.
    /// </summary>
    public sealed class KafkaClientConfig : ConfluentClientConfigProxy<ClientConfig>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaClientConfig" /> class.
        /// </summary>
        public KafkaClientConfig()
            : base(new ClientConfig())
        {
        }

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public override void Validate()
        {
            // Don't validate anything, leave it to the KafkaProducerConfig and KafkaConsumerConfig
        }
    }
}
