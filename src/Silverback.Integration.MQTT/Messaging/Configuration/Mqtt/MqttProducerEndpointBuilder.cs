// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <inheritdoc cref="IMqttProducerEndpointBuilder" />
    public class MqttProducerEndpointBuilder
        : ProducerEndpointBuilder<MqttProducerEndpoint, IMqttProducerEndpointBuilder>, IMqttProducerEndpointBuilder
    {
        private readonly MqttClientConfig _clientConfig;

        private string? _topicName;

        private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

        private bool? _retain;

        private uint? _messageExpiryInterval;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="MqttClientConfig" />.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public MqttProducerEndpointBuilder(
            MqttClientConfig clientConfig,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(endpointsConfigurationBuilder)
        {
            _clientConfig = clientConfig;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IMqttProducerEndpointBuilder This => this;

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo" />
        public IMqttProducerEndpointBuilder ProduceTo(string topicName)
        {
            Check.NotEmpty(topicName, nameof(topicName));

            _topicName = topicName;

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.WithQualityOfServiceLevel" />
        public IMqttProducerEndpointBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
        {
            _qualityOfServiceLevel = qosLevel;
            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.WithAtMostOnceQoS" />
        public IMqttProducerEndpointBuilder WithAtMostOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.WithAtLeastOnceQoS" />
        public IMqttProducerEndpointBuilder WithAtLeastOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.WithExactlyOnceQoS" />
        public IMqttProducerEndpointBuilder WithExactlyOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.Retain" />
        public IMqttProducerEndpointBuilder Retain()
        {
            _retain = true;
            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.WithMessageExpiration" />
        public IMqttProducerEndpointBuilder WithMessageExpiration(TimeSpan messageExpiryInterval)
        {
            Check.Range(
                messageExpiryInterval,
                nameof(messageExpiryInterval),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(uint.MaxValue));

            _messageExpiryInterval = (uint)messageExpiryInterval.TotalSeconds;
            return this;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override MqttProducerEndpoint CreateEndpoint()
        {
            if (string.IsNullOrEmpty(_topicName))
                throw new EndpointConfigurationException("Topic name not set.");

            var endpoint = new MqttProducerEndpoint(_topicName)
            {
                Configuration = _clientConfig
            };

            if (_qualityOfServiceLevel != null)
                endpoint.QualityOfServiceLevel = _qualityOfServiceLevel.Value;

            if (_retain != null)
                endpoint.Retain = _retain.Value;

            if (_messageExpiryInterval != null)
                endpoint.MessageExpiryInterval = _messageExpiryInterval.Value;

            return endpoint;
        }
    }
}
