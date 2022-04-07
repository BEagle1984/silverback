// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
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

            IEnumerable<IProducerEndpoint> endpointsToRegister;

            if (endpointsConfigurationBuilder.GetOutboundRoutingConfiguration()
                .IdempotentEndpointRegistration)
            {
                endpointsToRegister =
                    endpointsConfigurationBuilder.FilterRegisteredEndpoints(endpoints, messageType);
            }
            else
            {
                endpointsToRegister = endpoints;
            }

            return endpointsConfigurationBuilder.AddOutbound(
                messageType,
                new StaticOutboundRouter(endpointsToRegister),
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
            var logger = serviceProvider.GetRequiredService<ISilverbackLogger<IEndpointsConfigurationBuilder>>();

            router.Endpoints.ForEach(endpoint => PreloadProducer(brokers, endpoint, logger));
        }

        private static void PreloadProducer(
            IBrokerCollection brokers,
            IProducerEndpoint endpoint,
            ISilverbackLogger logger)
        {
            if (!endpoint.IsValid(logger))
                return;

            brokers.GetProducer(endpoint);
        }

        private static IEnumerable<IProducerEndpoint> FilterRegisteredEndpoints(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IEnumerable<IProducerEndpoint> endpoints,
            Type messageType)
        {
            var endpointsToRegister = new List<IProducerEndpoint>();

            var registeredStaticEndpoints = endpointsConfigurationBuilder
                .GetOutboundRoutingConfiguration()
                .Routes.Where(route => route.MessageType == messageType)
                .Select(route => route.GetOutboundRouter(endpointsConfigurationBuilder.ServiceProvider))
                .OfType<StaticOutboundRouter>()
                .SelectMany(router => router.Endpoints)
                .ToList();

            foreach (var endpoint in endpoints)
            {
                // There can only be one matching already registered endpoint.
                // Because when that one was registered it was also checked for already
                // existing registrations and, thus, would not have been added.
                // The only way this could be broken is when a user registers a
                // StaticOutboundRouter with endpoints explicitly - which is unlikely
                // and would cause SingleOrDefault to throw.
                IProducerEndpoint? registeredEndpoint =
                    registeredStaticEndpoints.SingleOrDefault(r => endpoint.Name == r.Name);

                if (registeredEndpoint is null)
                {
                    endpointsToRegister.Add(endpoint);
                    break;
                }

                if (!endpoint.Equals(registeredEndpoint))
                {
                    throw new EndpointConfigurationException(
                        $"An endpoint '{endpoint.Name}' for message type '{messageType.FullName}' but with a different configuration is already registered.");
                }
            }

            return endpointsToRegister;
        }

        private static IOutboundRoutingConfiguration GetOutboundRoutingConfiguration(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder) =>
            endpointsConfigurationBuilder.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
    }
}
