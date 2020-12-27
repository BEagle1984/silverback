// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Exposes the methods to configure the connection to Kafka and add the inbound and outbound endpoints.
    /// </summary>
    public interface IKafkaEndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        /// <summary>
        ///     Configures the Kafka client properties that are shared between the producers and consumers.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaEndpointsConfigurationBuilder Configure(Action<KafkaClientConfig> configAction);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="routerFunction">
        ///     The <see cref="KafkaOutboundEndpointRouter{TMessage}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            DictionaryOutboundRouter<object, KafkaProducerEndpoint>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="routerFunction">
        ///     The <see cref="KafkaOutboundEndpointRouter{TMessage}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            DictionaryOutboundRouter<TMessage, KafkaProducerEndpoint>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="routerFunction">
        ///     The <see cref="KafkaOutboundEndpointRouter{TMessage}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            DictionaryOutboundRouter<object, KafkaProducerEndpoint>.SingleEndpointRouterFunction
                routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="routerFunction">
        ///     The <see cref="KafkaOutboundEndpointRouter{TMessage}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
            DictionaryOutboundRouter<TMessage, KafkaProducerEndpoint>.SingleEndpointRouterFunction
                routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Kafka topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction,
            bool preloadProducers = true);

        /// <summary>
        ///     Adds an inbound endpoint to consume from a Kafka topic.
        /// </summary>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaConsumerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <param name="consumersCount">
        ///     The number of consumers to be instantiated. The default is 1.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IKafkaEndpointsConfigurationBuilder AddInbound(
            Action<IKafkaConsumerEndpointBuilder> endpointBuilderAction,
            int consumersCount = 1);
    }
}
