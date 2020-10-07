// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Outbound;
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
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints);
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
            IEnumerable<IProducerEndpoint> endpoints)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), endpoints);
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
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IEnumerable<IProducerEndpoint> endpoints)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoints, nameof(endpoints));

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                new StaticOutboundRouter(endpoints));
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

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), typeof(TRouter));
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
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IOutboundRouter<TMessage> router)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(router, nameof(router));

            return endpointsConfigurationBuilder.AddOutbound(typeof(TMessage), router);
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
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            Type routerType)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration().Add(
                messageType,
                serviceProvider => (IOutboundRouter)serviceProvider.GetRequiredService(routerType));

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
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddOutbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Type messageType,
            IOutboundRouter router)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(router, nameof(router));

            endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .Add(messageType, _ => router);

            return endpointsConfigurationBuilder;
        }

        private static IOutboundRoutingConfiguration GetOutboundRoutingConfiguration(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder) =>
            endpointsConfigurationBuilder.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
    }
}
