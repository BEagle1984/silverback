// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <inheritdoc cref="IKafkaConsumerEndpointBuilder" />
    public class KafkaConsumerEndpointBuilder
        : ConsumerEndpointBuilder<KafkaConsumerEndpoint, IKafkaConsumerEndpointBuilder>, IKafkaConsumerEndpointBuilder
    {
        private readonly KafkaClientConfig? _clientConfig;

        private string[]? _topicNames;

        private Action<KafkaConsumerConfig>? _configAction;

        private bool? _processPartitionsIndependently;

        private int? _maxDegreeOfParallelism;

        private int? _backpressureLimit;

        private Func<Error, KafkaConsumer, bool>? _kafkaErrorHandler;

        private Action<CommittedOffsets, KafkaConsumer>? _offsetsCommittedHandler;

        private Func<IReadOnlyCollection<TopicPartition>, KafkaConsumer, IEnumerable<TopicPartitionOffset>>?
            _partitionsAssignedHandler;

        private Action<IReadOnlyCollection<TopicPartitionOffset>, KafkaConsumer>? _partitionsRevokedHandler;

        private Action<KafkaStatistics, string, KafkaConsumer>? _statisticsHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public KafkaConsumerEndpointBuilder(
            KafkaClientConfig? clientConfig = null,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(endpointsConfigurationBuilder)
        {
            _clientConfig = clientConfig;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IKafkaConsumerEndpointBuilder This => this;

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(params string[] topicNames)
        {
            Check.HasNoEmpties(topicNames, nameof(topicNames));

            _topicNames = topicNames;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.Configure" />
        public IKafkaConsumerEndpointBuilder Configure(Action<KafkaConsumerConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            _configAction = configAction;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ProcessPartitionsIndependently" />
        public IKafkaConsumerEndpointBuilder ProcessPartitionsIndependently()
        {
            _processPartitionsIndependently = true;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ProcessAllPartitionsTogether" />
        public IKafkaConsumerEndpointBuilder ProcessAllPartitionsTogether()
        {
            _processPartitionsIndependently = false;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.LimitParallelism" />
        public IKafkaConsumerEndpointBuilder LimitParallelism(int maxDegreeOfParallelism)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.LimitBackpressure" />
        public IKafkaConsumerEndpointBuilder LimitBackpressure(int backpressureLimit)
        {
            _backpressureLimit = backpressureLimit;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.OnKafkaError" />
        public IKafkaConsumerEndpointBuilder OnKafkaError(Func<Error, KafkaConsumer, bool> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _kafkaErrorHandler = handler;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.OnOffsetsCommitted" />
        public IKafkaConsumerEndpointBuilder OnOffsetsCommitted(Action<CommittedOffsets, KafkaConsumer> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _offsetsCommittedHandler = handler;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.OnPartitionsAssigned" />
        public IKafkaConsumerEndpointBuilder OnPartitionsAssigned(
            Func<IReadOnlyCollection<TopicPartition>, KafkaConsumer, IEnumerable<TopicPartitionOffset>> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _partitionsAssignedHandler = handler;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.OnPartitionsRevoked" />
        public IKafkaConsumerEndpointBuilder OnPartitionsRevoked(
            Action<IReadOnlyCollection<TopicPartitionOffset>, KafkaConsumer> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _partitionsRevokedHandler = handler;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.OnStatisticsReceived" />
        public IKafkaConsumerEndpointBuilder OnStatisticsReceived(
            Action<KafkaStatistics, string, KafkaConsumer> handler)
        {
            Check.NotNull(handler, nameof(handler));

            _statisticsHandler = handler;

            return this;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override KafkaConsumerEndpoint CreateEndpoint()
        {
            if (_topicNames == null || _topicNames.Any(string.IsNullOrEmpty))
                throw new EndpointConfigurationException("Topic name not set.");

            var endpoint = new KafkaConsumerEndpoint(_topicNames, _clientConfig);

            _configAction?.Invoke(endpoint.Configuration);

            if (_processPartitionsIndependently != null)
                endpoint.ProcessPartitionsIndependently = _processPartitionsIndependently.Value;

            if (_maxDegreeOfParallelism != null)
                endpoint.MaxDegreeOfParallelism = _maxDegreeOfParallelism.Value;

            if (_backpressureLimit != null)
                endpoint.BackpressureLimit = _backpressureLimit.Value;

            endpoint.Events.ErrorHandler = _kafkaErrorHandler;
            endpoint.Events.OffsetsCommittedHandler = _offsetsCommittedHandler;
            endpoint.Events.PartitionsAssignedHandler = _partitionsAssignedHandler;
            endpoint.Events.PartitionsRevokedHandler = _partitionsRevokedHandler;
            endpoint.Events.StatisticsHandler = _statisticsHandler;

            return endpoint;
        }
    }
}
