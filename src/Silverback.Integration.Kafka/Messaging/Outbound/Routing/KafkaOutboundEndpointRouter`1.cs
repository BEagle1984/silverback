// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Routes the outbound messages to one or multiple Kafka endpoints.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be routed.
    /// </typeparam>
    public class KafkaOutboundEndpointRouter<TMessage>
        : DictionaryOutboundRouter<TMessage, KafkaProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOutboundEndpointRouter{TMessage}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="DictionaryOutboundRouter{TMessage,TEndpoint}.SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaOutboundEndpointRouter(
            SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            KafkaClientConfig? clientConfig = null)
            : base(routerFunction, BuildEndpointsDictionary(endpointBuilderActions, clientConfig))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOutboundEndpointRouter{TMessage}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="DictionaryOutboundRouter{TMessage,TEndpoint}.RouterFunction" />.
        /// </param>
        /// <param name="endpointBuilderActions">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the key of each endpoint and the
        ///     <see cref="Action{T}" /> to be invoked to build them.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaOutboundEndpointRouter(
            RouterFunction routerFunction,
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            KafkaClientConfig? clientConfig = null)
            : base(routerFunction, BuildEndpointsDictionary(endpointBuilderActions, clientConfig))
        {
        }

        private static IReadOnlyDictionary<string, KafkaProducerEndpoint> BuildEndpointsDictionary(
            IReadOnlyDictionary<string, Action<IKafkaProducerEndpointBuilder>> endpointBuilderActions,
            KafkaClientConfig? clientConfig) =>
            Check.NotNull(endpointBuilderActions, nameof(endpointBuilderActions))
                .ToDictionary(
                    pair => pair.Key,
                    pair => BuildEndpoint(pair.Value, clientConfig));

        private static KafkaProducerEndpoint BuildEndpoint(
            Action<IKafkaProducerEndpointBuilder> builderAction,
            KafkaClientConfig? clientConfig)
        {
            var builder = new KafkaProducerEndpointBuilder(clientConfig);

            builderAction.Invoke(builder);

            return builder.Build();
        }
    }
}
