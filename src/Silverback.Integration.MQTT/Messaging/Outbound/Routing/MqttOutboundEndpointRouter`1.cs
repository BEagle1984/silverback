// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Routes the outbound messages to one or multiple MQTT endpoints.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be routed.
    /// </typeparam>
    public class MqttOutboundEndpointRouter<TMessage>
        : DictionaryOutboundRouter<TMessage, MqttProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttOutboundEndpointRouter{TMessage}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="DictionaryOutboundRouter{TMessage,TEndpoint}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="MqttClientConfig" />.
        /// </param>
        /// <param name="mqttEventsHandlers">
        ///     The <see cref="MqttEventsHandlers"/>.
        /// </param>
        public MqttOutboundEndpointRouter(
            SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions,
            MqttClientConfig clientConfig,
            MqttEventsHandlers mqttEventsHandlers)
            : base(routerFunction, BuildEndpointsDictionary(endpointBuilderActions, clientConfig, mqttEventsHandlers))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttOutboundEndpointRouter{TMessage}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="DictionaryOutboundRouter{TMessage,TEndpoint}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="MqttClientConfig" />.
        /// </param>
        /// <param name="mqttEventsHandlers">
        ///     The <see cref="MqttEventsHandlers"/>.
        /// </param>
        public MqttOutboundEndpointRouter(
            RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions,
            MqttClientConfig clientConfig,
            MqttEventsHandlers mqttEventsHandlers)
            : base(routerFunction, BuildEndpointsDictionary(endpointBuilderActions, clientConfig, mqttEventsHandlers))
        {
        }

        private static Dictionary<string, MqttProducerEndpoint> BuildEndpointsDictionary(
            IReadOnlyDictionary<string, Action<IMqttProducerEndpointBuilder>> endpointBuilderActions,
            MqttClientConfig clientConfig,
            MqttEventsHandlers mqttEventsHandlers)
        {
            return Check.NotNull(endpointBuilderActions, nameof(endpointBuilderActions))
                .ToDictionary(
                    pair => pair.Key,
                    pair => BuildEndpoint(pair.Value, clientConfig, mqttEventsHandlers));
        }

        private static MqttProducerEndpoint BuildEndpoint(
            Action<IMqttProducerEndpointBuilder> builderAction,
            MqttClientConfig clientConfig,
            MqttEventsHandlers mqttEventsHandlers)
        {
            var builder = new MqttProducerEndpointBuilder(clientConfig, mqttEventsHandlers);

            builderAction.Invoke(builder);

            return builder.Build();
        }
    }
}
