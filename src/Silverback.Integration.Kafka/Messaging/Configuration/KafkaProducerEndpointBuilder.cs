// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IKafkaProducerEndpointBuilder" />
    public class KafkaProducerEndpointBuilder
        : ProducerEndpointBuilder<KafkaProducerEndpoint, IKafkaProducerEndpointBuilder>, IKafkaProducerEndpointBuilder
    {
        private readonly KafkaClientConfig? _clientConfig;

        private string? _topicName;

        private Action<KafkaProducerConfig>? _configAction;

        private Action<KafkaStatistics, string, KafkaProducer>? _statisticsHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The existing <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpointBuilder(KafkaClientConfig? clientConfig = null)
        {
            _clientConfig = clientConfig;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IKafkaProducerEndpointBuilder This => this;

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo" />
        public IKafkaProducerEndpointBuilder ProduceTo(string topicName)
        {
            Check.NotEmpty(topicName, nameof(topicName));

            _topicName = topicName;

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.Configure" />
        public IKafkaProducerEndpointBuilder Configure(Action<KafkaProducerConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            _configAction = configAction;

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.OnStatisticsReceived" />
        public IKafkaProducerEndpointBuilder OnStatisticsReceived(
            Action<KafkaStatistics, string, KafkaProducer> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _statisticsHandler = handler;

            return this;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override KafkaProducerEndpoint CreateEndpoint()
        {
            if (string.IsNullOrEmpty(_topicName))
                throw new EndpointConfigurationException("Topic name not set.");

            var endpoint = new KafkaProducerEndpoint(_topicName, _clientConfig);

            _configAction?.Invoke(endpoint.Configuration);

            endpoint.Events.StatisticsHandler = _statisticsHandler;

            return endpoint;
        }
    }
}
