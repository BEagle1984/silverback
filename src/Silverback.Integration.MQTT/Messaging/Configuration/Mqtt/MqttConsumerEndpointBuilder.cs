// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <inheritdoc cref="IMqttConsumerEndpointBuilder" />
    public class MqttConsumerEndpointBuilder
        : ConsumerEndpointBuilder<MqttConsumerEndpoint, IMqttConsumerEndpointBuilder>, IMqttConsumerEndpointBuilder
    {
        private MqttClientConfig _clientConfig;

        private string[]? _topicNames;

        private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

        private int? _maxDegreeOfParallelism;

        private int? _backpressureLimit;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttConsumerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="MqttClientConfig" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the message being consumed.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public MqttConsumerEndpointBuilder(
            MqttClientConfig clientConfig,
            Type? messageType = null,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(messageType, endpointsConfigurationBuilder)
        {
            _clientConfig = clientConfig;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IMqttConsumerEndpointBuilder This => this;

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.ConsumeFrom" />
        public IMqttConsumerEndpointBuilder ConsumeFrom(params string[] topics)
        {
            Check.HasNoEmpties(topics, nameof(topics));

            _topicNames = topics;

            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.Configure(Action{MqttClientConfig})" />
        public IMqttConsumerEndpointBuilder Configure(Action<MqttClientConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            configAction.Invoke(_clientConfig);

            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.Configure(Action{IMqttClientConfigBuilder})" />
        public IMqttConsumerEndpointBuilder Configure(Action<IMqttClientConfigBuilder> configBuilderAction)
        {
            Check.NotNull(configBuilderAction, nameof(configBuilderAction));

            var configBuilder = new MqttClientConfigBuilder(
                _clientConfig,
                EndpointsConfigurationBuilder?.ServiceProvider);
            configBuilderAction.Invoke(configBuilder);

            _clientConfig = configBuilder.Build();

            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.WithQualityOfServiceLevel" />
        public IMqttConsumerEndpointBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
        {
            _qualityOfServiceLevel = qosLevel;
            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.WithAtMostOnceQoS" />
        public IMqttConsumerEndpointBuilder WithAtMostOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.WithAtLeastOnceQoS" />
        public IMqttConsumerEndpointBuilder WithAtLeastOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.WithExactlyOnceQoS" />
        public IMqttConsumerEndpointBuilder WithExactlyOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.EnableParallelProcessing" />
        public IMqttConsumerEndpointBuilder EnableParallelProcessing(int maxDegreeOfParallelism)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;

            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.DisableParallelProcessing" />
        public IMqttConsumerEndpointBuilder DisableParallelProcessing()
        {
            _maxDegreeOfParallelism = 1;

            return this;
        }

        /// <inheritdoc cref="IMqttConsumerEndpointBuilder.LimitBackpressure" />
        public IMqttConsumerEndpointBuilder LimitBackpressure(int backpressureLimit)
        {
            _backpressureLimit = backpressureLimit;

            return this;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override MqttConsumerEndpoint CreateEndpoint()
        {
            var endpoint = new MqttConsumerEndpoint(_topicNames ?? Array.Empty<string>())
            {
                Configuration = _clientConfig,
            };

            if (_qualityOfServiceLevel != null)
                endpoint.QualityOfServiceLevel = _qualityOfServiceLevel.Value;

            if (_maxDegreeOfParallelism != null)
                endpoint.MaxDegreeOfParallelism = _maxDegreeOfParallelism.Value;

            if (_backpressureLimit != null)
                endpoint.BackpressureLimit = _backpressureLimit.Value;

            return endpoint;
        }
    }
}
