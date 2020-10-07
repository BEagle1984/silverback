// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddOutboundConnector</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddOutboundConnectorExtensions
    {
        /// <summary>
        ///     Adds a connector to publish the messages to the configured message broker.
        /// </summary>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IOutboundConnector" /> implementation to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboundConnector<TConnector>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TConnector : class, IOutboundConnector
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            if (!brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<IOutboundConnector>())
            {
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
                brokerOptionsBuilder.SilverbackBuilder
                    .AddScopedBehavior<OutboundRouterBehavior>()
                    .AddScopedBehavior<ProduceBehavior>();
            }

            brokerOptionsBuilder.SilverbackBuilder.Services.AddScoped<IOutboundConnector, TConnector>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a connector to publish the messages to the configured message broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboundConnector(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOutboundConnector<OutboundConnector>();
        }

        /// <summary>
        ///     Adds a connector to publish the messages to the configured message broker. This implementation
        ///     stores the outbound messages into an intermediate queue.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDeferredOutboundConnector(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddOutboundConnector<DeferredOutboundConnector>();
            brokerOptionsBuilder.SilverbackBuilder.AddScopedSubscriber<DeferredOutboundConnectorTransactionManager>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a connector to publish the messages to the configured message broker. This implementation
        ///     stores the outbound messages into an intermediate queue.
        /// </summary>
        /// <typeparam name="TQueueWriter">
        ///     The type of the <see cref="IOutboundQueueWriter" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDeferredOutboundConnector<TQueueWriter>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TQueueWriter : class, IOutboundQueueWriter
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddDeferredOutboundConnector();
            brokerOptionsBuilder.SilverbackBuilder.Services.AddScoped<IOutboundQueueWriter, TQueueWriter>();
            brokerOptionsBuilder.SilverbackBuilder.Services.AddScoped<TransactionalOutboxBroker>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a connector to publish the messages to the configured message broker. This implementation
        ///     stores the outbound messages into an intermediate queue in the database and it is therefore fully
        ///     transactional.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbOutboundConnector(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddDeferredOutboundConnector<DbOutboundQueueWriter>();

            return brokerOptionsBuilder;
        }
    }
}
