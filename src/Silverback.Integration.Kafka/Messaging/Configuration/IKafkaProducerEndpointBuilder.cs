// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="KafkaProducerEndpoint" />.
    /// </summary>
    public interface IKafkaProducerEndpointBuilder : IProducerEndpointBuilder<IKafkaProducerEndpointBuilder>
    {
        /// <summary>
        ///     Specifies the name of the topic.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo(string topicName);

        /// <summary>
        ///     Configures the Kafka client properties.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder Configure(Action<KafkaProducerConfig> configAction);

        /// <summary>
        ///     <para>
        ///         Sets the handler to call on statistics events.
        ///     </para>
        ///     <para>
        ///         You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
        ///         configuration property (disabled by default).
        ///     </para>
        /// </summary>
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder OnStatisticsReceived(Action<KafkaStatistics, string, KafkaProducer> handler);
    }
}
