// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <inheritdoc cref="IKafkaConsumerEndpointBuilder" />
    public class KafkaConsumerEndpointBuilder
        : ConsumerEndpointBuilder<KafkaConsumerEndpoint, IKafkaConsumerEndpointBuilder>,
            IKafkaConsumerEndpointBuilder
    {
        private readonly KafkaClientConfig? _clientConfig;

        private string[]? _topicNames;

        private TopicPartitionOffset[]? _topicPartitionOffsets;

        private Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            _topicPartitionsResolver;

        private Action<KafkaConsumerConfig>? _configAction;

        private bool? _processPartitionsIndependently;

        private int? _maxDegreeOfParallelism;

        private int? _backpressureLimit;

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

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(string[])" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(params string[] topicNames)
        {
            Check.HasNoEmpties(topicNames, nameof(topicNames));

            _topicNames = topicNames;
            _topicPartitionOffsets = null;
            _topicPartitionsResolver = null;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(TopicPartition[])" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(params TopicPartition[] topicPartitions)
        {
            Check.HasNoNulls(topicPartitions, nameof(topicPartitions));

            _topicPartitionOffsets = topicPartitions
                .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset)).ToArray();
            _topicNames = null;
            _topicPartitionsResolver = null;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(TopicPartitionOffset[])" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(params TopicPartitionOffset[] topicPartitions)
        {
            Check.HasNoNulls(topicPartitions, nameof(topicPartitions));

            _topicPartitionOffsets = topicPartitions;
            _topicNames = null;
            _topicPartitionsResolver = null;

            return this;
        }

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(string, Func{IReadOnlyCollection{TopicPartition},IEnumerable{TopicPartition}})" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver) =>
            ConsumeFrom(new[] { topicName }, topicPartitionsResolver);

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(string, Func{IReadOnlyCollection{TopicPartition},IEnumerable{TopicPartitionOffset}})" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver) =>
            ConsumeFrom(new[] { topicName }, topicPartitionsResolver);

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(string[], Func{IReadOnlyCollection{TopicPartition},IEnumerable{TopicPartition}})" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver) =>
            ConsumeFrom(
                topicNames,
                topicPartitions => topicPartitionsResolver(topicPartitions)
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset)));

        /// <inheritdoc cref="IKafkaConsumerEndpointBuilder.ConsumeFrom(string[], Func{IReadOnlyCollection{TopicPartition},IEnumerable{TopicPartitionOffset}})" />
        public IKafkaConsumerEndpointBuilder ConsumeFrom(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver)
        {
            _topicNames = topicNames;
            _topicPartitionsResolver = topicPartitionsResolver;
            _topicPartitionOffsets = null;

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

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override KafkaConsumerEndpoint CreateEndpoint()
        {
            KafkaConsumerEndpoint endpoint;

            if (_topicNames != null && _topicPartitionsResolver != null)
                endpoint = new KafkaConsumerEndpoint(_topicNames, _topicPartitionsResolver, _clientConfig);
            else if (_topicPartitionOffsets != null)
                endpoint = new KafkaConsumerEndpoint(_topicPartitionOffsets, _clientConfig);
            else
                endpoint = new KafkaConsumerEndpoint(_topicNames ?? Array.Empty<string>(), _clientConfig);

            _configAction?.Invoke(endpoint.Configuration);

            if (_processPartitionsIndependently != null)
                endpoint.ProcessPartitionsIndependently = _processPartitionsIndependently.Value;

            if (_maxDegreeOfParallelism != null)
                endpoint.MaxDegreeOfParallelism = _maxDegreeOfParallelism.Value;

            if (_backpressureLimit != null)
                endpoint.BackpressureLimit = _backpressureLimit.Value;

            return endpoint;
        }
    }
}
