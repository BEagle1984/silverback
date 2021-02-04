// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <inheritdoc cref="IMqttProducerEndpointBuilder" />
    public class MqttProducerEndpointBuilder
        : ProducerEndpointBuilder<MqttProducerEndpoint, IMqttProducerEndpointBuilder>,
            IMqttProducerEndpointBuilder
    {
        private MqttClientConfig _clientConfig;

        private MqttEventsHandlers _mqttEventsHandlers;

        private Func<MqttProducerEndpoint>? _endpointFactory;

        private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

        private bool? _retain;

        private uint? _messageExpiryInterval;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="MqttClientConfig" />.
        /// </param>
        /// <param name="mqttEventsHandlers">
        ///     The <see cref="MqttEventsHandlers"/>.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public MqttProducerEndpointBuilder(
            MqttClientConfig clientConfig,
            MqttEventsHandlers mqttEventsHandlers,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(endpointsConfigurationBuilder)
        {
            _clientConfig = clientConfig;
            _mqttEventsHandlers = mqttEventsHandlers;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IMqttProducerEndpointBuilder This => this;

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo(string)" />
        public IMqttProducerEndpointBuilder ProduceTo(string topicName)
        {
            Check.NotEmpty(topicName, nameof(topicName));

            _endpointFactory = () => new MqttProducerEndpoint(topicName);

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo(Func{IOutboundEnvelope, string})" />
        public IMqttProducerEndpointBuilder ProduceTo(Func<IOutboundEnvelope, string> topicNameFunction)
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new MqttProducerEndpoint(topicNameFunction);

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo{TMessage}(Func{IOutboundEnvelope{TMessage}, string})" />
        public IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, string> topicNameFunction)
            where TMessage : class
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new MqttProducerEndpoint(
                envelope => topicNameFunction.Invoke((IOutboundEnvelope<TMessage>)envelope));

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo(Func{IOutboundEnvelope, IServiceProvider, string})" />
        public IMqttProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, IServiceProvider, string> topicNameFunction)
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new MqttProducerEndpoint(topicNameFunction);

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo{TMessage}(Func{IOutboundEnvelope{TMessage}, IServiceProvider, string})" />
        public IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, string> topicNameFunction)
            where TMessage : class
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new MqttProducerEndpoint(
                (envelope, serviceProvider) =>
                    topicNameFunction.Invoke((IOutboundEnvelope<TMessage>)envelope, serviceProvider));

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo(string, Func{IOutboundEnvelope, string[]})" />
        public IMqttProducerEndpointBuilder ProduceTo(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction)
        {
            Check.NotEmpty(topicNameFormatString, nameof(topicNameFormatString));
            Check.NotNull(topicNameArgumentsFunction, nameof(topicNameArgumentsFunction));

            _endpointFactory = () => new MqttProducerEndpoint(
                topicNameFormatString,
                topicNameArgumentsFunction);

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.ProduceTo{TMessage}(string, Func{IOutboundEnvelope{TMessage}, string[]})" />
        public IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            string topicNameFormatString,
            Func<IOutboundEnvelope<TMessage>, string[]> topicNameArgumentsFunction)
            where TMessage : class
        {
            Check.NotEmpty(topicNameFormatString, nameof(topicNameFormatString));
            Check.NotNull(topicNameArgumentsFunction, nameof(topicNameArgumentsFunction));

            _endpointFactory = () => new MqttProducerEndpoint(
                topicNameFormatString,
                envelope => topicNameArgumentsFunction.Invoke((IOutboundEnvelope<TMessage>)envelope));

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.UseEndpointNameResolver{TResolver}" />
        public IMqttProducerEndpointBuilder UseEndpointNameResolver<TResolver>()
            where TResolver : IProducerEndpointNameResolver
        {
            _endpointFactory = () => new MqttProducerEndpoint(typeof(TResolver));

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.Configure(Action{MqttClientConfig})" />
        public IMqttProducerEndpointBuilder Configure(Action<MqttClientConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            configAction.Invoke(_clientConfig);

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.Configure(Action{IMqttClientConfigBuilder})" />
        public IMqttProducerEndpointBuilder Configure(Action<IMqttClientConfigBuilder> configBuilderAction)
        {
            Check.NotNull(configBuilderAction, nameof(configBuilderAction));

            var configBuilder = new MqttClientConfigBuilder(
                _clientConfig,
                EndpointsConfigurationBuilder?.ServiceProvider);
            configBuilderAction.Invoke(configBuilder);

            _clientConfig = configBuilder.Build();

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.BindEvents(Action{IMqttEventsHandlersBuilder})"/>
        public IMqttProducerEndpointBuilder BindEvents(Action<IMqttEventsHandlersBuilder> eventsHandlersBuilderAction)
        {
            Check.NotNull(eventsHandlersBuilderAction, nameof(eventsHandlersBuilderAction));

            var eventsBuilder = new MqttEventsHandlersBuilder(_mqttEventsHandlers);
            eventsHandlersBuilderAction.Invoke(eventsBuilder);

            _mqttEventsHandlers = eventsBuilder.Build();

            return this;
        }

        /// <inheritdoc cref="IMqttProducerEndpointBuilder.BindEvents(Action{MqttEventsHandlers})"/>
        public IMqttProducerEndpointBuilder BindEvents(Action<MqttEventsHandlers> eventsHandlersAction)
        {
            Check.NotNull(eventsHandlersAction, nameof(eventsHandlersAction));

            eventsHandlersAction.Invoke(_mqttEventsHandlers);

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
            if (_endpointFactory == null)
            {
                throw new EndpointConfigurationException(
                    "Topic name not set. Use ProduceTo or UseEndpointNameResolver to set it.");
            }

            var endpoint = _endpointFactory.Invoke();

            endpoint.Configuration = _clientConfig;
            endpoint.EventsHandlers = _mqttEventsHandlers;

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
