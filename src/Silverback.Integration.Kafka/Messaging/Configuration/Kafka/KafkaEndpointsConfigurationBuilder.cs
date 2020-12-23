// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka
{
    internal class KafkaEndpointsConfigurationBuilder : IKafkaEndpointsConfigurationBuilder
    {
        private readonly IEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

        public KafkaEndpointsConfigurationBuilder(IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
        {
            _endpointsConfigurationBuilder = endpointsConfigurationBuilder;
        }

        public IServiceProvider ServiceProvider => _endpointsConfigurationBuilder.ServiceProvider;

        internal KafkaClientConfig ClientConfig { get; } = new();

        public IKafkaEndpointsConfigurationBuilder Configure(Action<KafkaClientConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            configAction.Invoke(ClientConfig);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction) =>
            AddOutbound(typeof(TMessage), endpointBuilderAction);

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            KafkaOutboundEndpointRouter<object>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                messageType,
                new KafkaOutboundEndpointRouter<object>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            KafkaOutboundEndpointRouter<TMessage>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                new KafkaOutboundEndpointRouter<TMessage>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            KafkaOutboundEndpointRouter<object>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                messageType,
                new KafkaOutboundEndpointRouter<object>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            KafkaOutboundEndpointRouter<TMessage>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions)
        {
            this.AddOutbound(
                new KafkaOutboundEndpointRouter<TMessage>(routerFunction, endpointBuilderActions, ClientConfig));

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction)
        {
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new KafkaProducerEndpointBuilder(ClientConfig, this);
            endpointBuilderAction.Invoke(builder);

            _endpointsConfigurationBuilder.AddOutbound(messageType, builder.Build());

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddInbound(
            Action<IKafkaConsumerEndpointBuilder> endpointBuilderAction,
            int consumersCount = 1)
        {
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new KafkaConsumerEndpointBuilder(ClientConfig, this);
            endpointBuilderAction.Invoke(builder);

            _endpointsConfigurationBuilder.AddInbound(builder.Build(), consumersCount);

            return this;
        }
    }
}
