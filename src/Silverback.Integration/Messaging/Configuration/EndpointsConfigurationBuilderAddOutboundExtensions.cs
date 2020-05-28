// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>
    ///         AddOutbound
    ///     </c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddOutboundExtensions
    {
        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint (topic).
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IProducerEndpoint endpoint)
            where TConnector : IOutboundConnector
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound<TMessage, TConnector>(new[] { endpoint });
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The endpoints (topics).
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            params IProducerEndpoint[] endpoints)
            where TConnector : IOutboundConnector
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints, typeof(TConnector));
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The endpoints (topics).
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IEnumerable<IProducerEndpoint> endpoints)
            where TConnector : IOutboundConnector
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints, typeof(TConnector));
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The endpoints (topics).
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

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint (topic).
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
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
            Type? outboundConnectorType = null)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound<TMessage>(
                new[] { endpoint },
                outboundConnectorType);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoints">
        ///     The endpoints (topics).
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
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
            Type? outboundConnectorType = null)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints, outboundConnectorType);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="messageType">
        ///     The type of the messages to be published to this endpoint.
        /// </param>
        /// <param name="endpoints">
        ///     The endpoints (topics).
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IEnumerable<IProducerEndpoint> endpoints,
            Type? outboundConnectorType = null)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                new StaticOutboundRouter(endpoints),
                outboundConnectorType);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TRouter">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination
        ///     endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        /// </typeparam>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter, TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
            where TRouter : IOutboundRouter<TMessage>
            where TConnector : IOutboundConnector
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), typeof(TRouter), typeof(TConnector));
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
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
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
            where TRouter : IOutboundRouter<TMessage>
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), typeof(TRouter), null);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="router">
        ///     The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be published to this endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IOutboundRouter<TMessage> router)
            where TConnector : IOutboundConnector
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), router, typeof(TConnector));
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
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
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IOutboundRouter<TMessage> router,
            Type? outboundConnectorType = null)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), router, outboundConnectorType);
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
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
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            Type routerType,
            Type? outboundConnectorType)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            var router = (IOutboundRouter)endpointsConfigurationBuilder.ServiceProvider.GetRequiredService(routerType);

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .Add(messageType, router, outboundConnectorType);

            return endpointsConfigurationBuilder;
        }

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
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
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IOutboundRouter router,
            Type? outboundConnectorType)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .Add(messageType, router, outboundConnectorType);

            return endpointsConfigurationBuilder;
        }

        /// <summary>
        ///     Enables the legacy behavior where the messages to be routed through an outbound connector are also
        ///     being published to the internal bus, to be locally subscribed. This is now disabled by default.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .PublishOutboundMessagesToInternalBus = true;

            return endpointsConfigurationBuilder;
        }

        private static IOutboundRoutingConfiguration GetOutboundRoutingConfiguration(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder) =>
            endpointsConfigurationBuilder.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
    }
}
