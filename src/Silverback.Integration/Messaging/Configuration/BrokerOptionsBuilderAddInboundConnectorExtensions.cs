// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.ErrorHandling;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddInboundConnector </c> and related methods to the
    ///     <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddInboundConnectorExtensions
    {
        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus.
        /// </summary>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IInboundConnector" /> implementation to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInboundConnector<TConnector>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TConnector : class, IInboundConnector
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            if (!brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<IInboundConnector>())
                brokerOptionsBuilder.SilverbackBuilder.Services.AddSingleton<IErrorPolicyHelper, ErrorPolicyHelper>();

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingleton<IInboundConnector, TConnector>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds the default connector to subscribe to a message broker and forward the incoming messages to the
        ///     internal bus.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInboundConnector(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddInboundConnector<InboundConnector>();
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation logs the incoming messages and prevents duplicated processing of the same
        ///     message.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddLoggedInboundConnector(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddInboundConnector<LoggedInboundConnector>();
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation logs the incoming messages and prevents duplicated processing of the same
        ///     message.
        /// </summary>
        /// <typeparam name="TLog">
        ///     The type of the <see cref="IInboundLog" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddLoggedInboundConnector<TLog>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TLog : class, IInboundLog
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddLoggedInboundConnector();
            brokerOptionsBuilder.SilverbackBuilder.Services.AddScoped<IInboundLog, TLog>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages and prevents duplicated
        ///     processing of the same message.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOffsetStoredInboundConnector(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddInboundConnector<OffsetStoredInboundConnector>();
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages and prevents duplicated
        ///     processing of the same message.
        /// </summary>
        /// <typeparam name="TStore">
        ///     The type of the <see cref="IOffsetStore" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOffsetStoredInboundConnector<TStore>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TStore : class, IOffsetStore
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddOffsetStoredInboundConnector();
            brokerOptionsBuilder.SilverbackBuilder.Services.AddScoped<IOffsetStore, TStore>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation logs the incoming messages in the database and prevents duplicated
        ///     processing of the same message.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbLoggedInboundConnector(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddLoggedInboundConnector<DbInboundLog>();
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages in the database and
        ///     prevents duplicated processing of the same message.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbOffsetStoredInboundConnector(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOffsetStoredInboundConnector<DbOffsetStore>();
        }
    }
}
