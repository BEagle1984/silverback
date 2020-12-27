// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>AddOutbound</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddOutboundExtensions
    {
        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The collection of <see cref="IProducerEndpoint" /> representing the destination topics or queues.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            params IProducerEndpoint[] endpoints)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint">
        ///     The <see cref="IProducerEndpoint" /> representing the destination topic or queue.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IProducerEndpoint endpoint,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoint, nameof(endpoint));

            return endpointsConfigurationBuilder.AddOutbound(
                typeof(TMessage),
                new[] { endpoint },
                preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The collection of <see cref="IProducerEndpoint" /> representing the destination topics or queues.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IEnumerable<IProducerEndpoint> endpoints,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints, preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpoint">
        ///     The <see cref="IProducerEndpoint" /> representing the destination topic or queue.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IProducerEndpoint endpoint,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpoint, nameof(endpoint));

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                new[] { endpoint },
                preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpoints">
        ///     The collection of <see cref="IProducerEndpoint" /> representing the destination topics or queues.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            params IProducerEndpoint[] endpoints)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                (IEnumerable<IProducerEndpoint>)endpoints);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpoints">
        ///     The collection of <see cref="IProducerEndpoint" /> representing the destination topics or queues.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IEnumerable<IProducerEndpoint> endpoints,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                new StaticOutboundRouter(endpoints),
                preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TRouter">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination
        ///     endpoint.
        /// </typeparam>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            bool preloadProducers = true)
            where TRouter : IOutboundRouter<TMessage>
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(
                typeof(TMessage),
                typeof(TRouter),
                preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="router">
        ///     The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IOutboundRouter<TMessage> router,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(router, nameof(router));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), router, preloadProducers);
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="routerType">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination
        ///     endpoint.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            Type routerType,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(routerType, nameof(routerType));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration().Add(
                messageType,
                serviceProvider => (IOutboundRouter)serviceProvider.GetRequiredService(routerType));

            if (preloadProducers)
            {
                var router = (IOutboundRouter)endpointsConfigurationBuilder.ServiceProvider
                    .GetRequiredService(routerType);

                PreloadProducers(router, endpointsConfigurationBuilder.ServiceProvider);
            }

            return endpointsConfigurationBuilder;
        }

        /// <summary>
        ///     Adds an outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="router">
        ///     The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.
        /// </param>
        /// <param name="preloadProducers">
        ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
        ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IOutboundRouter router,
            bool preloadProducers = true)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(messageType, nameof(messageType));
            Check.NotNull(router, nameof(router));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .Add(messageType, _ => router);

            if (preloadProducers)
                PreloadProducers(router, endpointsConfigurationBuilder.ServiceProvider);

            return endpointsConfigurationBuilder;
        }

        private static void PreloadProducers(IOutboundRouter router, IServiceProvider serviceProvider)
        {
            var brokers = serviceProvider.GetRequiredService<IBrokerCollection>();

            router.Endpoints.ForEach(endpoint => brokers.GetProducer(endpoint));
        }

        private static IOutboundRoutingConfiguration GetOutboundRoutingConfiguration(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder) =>
            endpointsConfigurationBuilder.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
    }
}
