// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    internal class MqttEndpointsConfigurationBuilder : IMqttEndpointsConfigurationBuilder
    {
        private readonly IEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

        public MqttEndpointsConfigurationBuilder(IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
        {
            _endpointsConfigurationBuilder = endpointsConfigurationBuilder;
        }

        public IServiceProvider ServiceProvider => _endpointsConfigurationBuilder.ServiceProvider;

        internal MqttClientConfig ClientConfig { get; private set; } = new();

        public IMqttEndpointsConfigurationBuilder Configure(Action<MqttClientConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            configAction.Invoke(ClientConfig);

            return this;
        }

        public IMqttEndpointsConfigurationBuilder Configure(Action<IMqttClientConfigBuilder> configBuilderAction)
        {
            Check.NotNull(configBuilderAction, nameof(configBuilderAction));

            var configBuilder = new MqttClientConfigBuilder(ServiceProvider);
            configBuilderAction.Invoke(configBuilder);

            ClientConfig = configBuilder.Build();

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            Action<IMqttProducerEndpointBuilder> endpointBuilderAction) =>
            AddOutbound(typeof(TMessage), endpointBuilderAction);

        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            MqttOutboundEndpointRouter<object>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                messageType,
                new MqttOutboundEndpointRouter<object>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            MqttOutboundEndpointRouter<TMessage>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                new MqttOutboundEndpointRouter<TMessage>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            MqttOutboundEndpointRouter<object>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                messageType,
                new MqttOutboundEndpointRouter<object>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            MqttOutboundEndpointRouter<TMessage>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                new MqttOutboundEndpointRouter<TMessage>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Action<IMqttProducerEndpointBuilder> endpointBuilderAction)
        {
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new MqttProducerEndpointBuilder(ClientConfig, this);
            endpointBuilderAction.Invoke(builder);

            _endpointsConfigurationBuilder.AddOutbound(messageType, builder.Build());

            return this;
        }

        public IMqttEndpointsConfigurationBuilder AddInbound(Action<IMqttConsumerEndpointBuilder> endpointBuilderAction)
        {
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new MqttConsumerEndpointBuilder(ClientConfig, this);
            endpointBuilderAction.Invoke(builder);

            _endpointsConfigurationBuilder.AddInbound(builder.Build());

            return this;
        }
    }
}
