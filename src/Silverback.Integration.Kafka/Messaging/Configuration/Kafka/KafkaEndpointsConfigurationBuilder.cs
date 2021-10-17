// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka
{
    internal sealed class KafkaEndpointsConfigurationBuilder : IKafkaEndpointsConfigurationBuilder
    {
        private readonly IEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

        public KafkaEndpointsConfigurationBuilder(
            IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
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
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction,
            bool preloadProducers = true) =>
            AddOutbound(typeof(TMessage), endpointBuilderAction, preloadProducers);

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            DictionaryOutboundRouter<object, KafkaProducerEndpoint>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true)
        {
            var router = new KafkaOutboundEndpointRouter<object>(
                routerFunction,
                endpointBuilderActions,
                ClientConfig);
            this.AddOutbound(messageType, router, preloadProducers);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            DictionaryOutboundRouter<TMessage, KafkaProducerEndpoint>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true)
        {
            var router = new KafkaOutboundEndpointRouter<TMessage>(
                routerFunction,
                endpointBuilderActions,
                ClientConfig);
            this.AddOutbound(router, preloadProducers);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            DictionaryOutboundRouter<object, KafkaProducerEndpoint>.SingleEndpointRouterFunction
                routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true)
        {
            var router = new KafkaOutboundEndpointRouter<object>(
                routerFunction,
                endpointBuilderActions,
                ClientConfig);
            this.AddOutbound(messageType, router, preloadProducers);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            DictionaryOutboundRouter<TMessage, KafkaProducerEndpoint>.SingleEndpointRouterFunction
                routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true)
        {
            var router = new KafkaOutboundEndpointRouter<TMessage>(
                routerFunction,
                endpointBuilderActions,
                ClientConfig);
            this.AddOutbound(router, preloadProducers);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction,
            bool preloadProducers = true)
        {
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new KafkaProducerEndpointBuilder(ClientConfig, this);
            endpointBuilderAction.Invoke(builder);

            this.AddOutbound(messageType, builder.Build(), preloadProducers);

            return this;
        }

        public IKafkaEndpointsConfigurationBuilder AddInbound(
            Action<IKafkaConsumerEndpointBuilder> endpointBuilderAction,
            int consumersCount = 1) =>
            AddInbound(null, endpointBuilderAction, consumersCount);

        public IKafkaEndpointsConfigurationBuilder AddInbound<TMessage>(
            Action<IKafkaConsumerEndpointBuilder> endpointBuilderAction,
            int consumersCount = 1) =>
            AddInbound(typeof(TMessage), endpointBuilderAction, consumersCount);

        public IKafkaEndpointsConfigurationBuilder AddInbound(
            Type? messageType,
            Action<IKafkaConsumerEndpointBuilder> endpointBuilderAction,
            int consumersCount = 1)
        {
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var builder = new KafkaConsumerEndpointBuilder(ClientConfig, messageType, this);
            builder.DeserializeJson();

            endpointBuilderAction.Invoke(builder);

            _endpointsConfigurationBuilder.AddInbound(builder.Build(), consumersCount);

            return this;
        }
    }
}
