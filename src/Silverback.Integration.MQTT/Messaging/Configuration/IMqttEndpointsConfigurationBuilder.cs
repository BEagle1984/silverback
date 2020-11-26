// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Exposes the methods to configure the connection to Mqtt and add the inbound and outbound endpoints.
    /// </summary>
    public interface IMqttEndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        /// <summary>
        ///     Configures the MQTT client properties that are shared between the producers and consumers.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttEndpointsConfigurationBuilder Configure(Action<MqttClientConfig> configAction);

        /// <summary>
        ///     Configures the MQTT client properties that are shared between the producers and consumers.
        /// </summary>
        /// <param name="configBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttClientConfigBuilder" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttEndpointsConfigurationBuilder Configure(Action<IMqttClientConfigBuilder> configBuilderAction);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            Action<IMqttProducerEndpointBuilder> endpointBuilderAction);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="routerFunction">
        ///     The <see cref="MqttOutboundEndpointRouter{TMessage}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            MqttOutboundEndpointRouter<object>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="routerFunction">
        ///     The <see cref="MqttOutboundEndpointRouter{TMessage}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            MqttOutboundEndpointRouter<TMessage>.RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="routerFunction">
        ///     The <see cref="MqttOutboundEndpointRouter{TMessage}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            MqttOutboundEndpointRouter<object>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="routerFunction">
        ///     The <see cref="MqttOutboundEndpointRouter{TMessage}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
            MqttOutboundEndpointRouter<TMessage>.SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions);

        /// <summary>
        ///     Adds an outbound endpoint to produce the specified message type to a Mqtt topic.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Action<IMqttProducerEndpointBuilder> endpointBuilderAction);

        /// <summary>
        ///     Adds an inbound endpoint to consume from a Mqtt topic.
        /// </summary>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttConsumerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public IMqttEndpointsConfigurationBuilder AddInbound(
            Action<IMqttConsumerEndpointBuilder> endpointBuilderAction);
    }
}
