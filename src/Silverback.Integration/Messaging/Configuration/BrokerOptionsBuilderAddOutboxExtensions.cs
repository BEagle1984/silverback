// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddOutbox</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddOutboxExtensions
    {
        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OutboxProduceStrategy" />.
        ///     </para>
        ///     <para>
        ///         The <see cref="OutboxProduceStrategy" /> stores the outbound messages into an intermediate outbox,
        ///         participating in the database transaction. The outbound messages become therefore transactional
        ///         with the side effects on the local database.
        ///     </para>
        /// </summary>
        /// <typeparam name="TOutboxWriter">
        ///     The type of the <see cref="IOutboxWriter" /> to be used.
        /// </typeparam>
        /// <typeparam name="TOutboxReader">
        ///     The type of the <see cref="IOutboxReader" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutbox<TOutboxWriter, TOutboxReader>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TOutboxWriter : class, IOutboxWriter
            where TOutboxReader : class, IOutboxReader
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder
                .AddScopedSubscriber<OutboxTransactionManager>()
                .Services
                .AddScoped<IOutboxWriter, TOutboxWriter>()
                .AddScoped<IOutboxReader, TOutboxReader>()
                .AddScoped<TransactionalOutboxBroker>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OutboxProduceStrategy" />.
        ///     </para>
        ///     <para>
        ///         The <see cref="OutboxProduceStrategy" /> stores the outbound messages into an intermediate outbox,
        ///         participating in the database transaction. The outbound messages become therefore transactional
        ///         with the side effects on the local database.
        ///     </para>
        /// </summary>
        /// <typeparam name="TOutbox">
        ///     The type implementing both the <see cref="IOutboxWriter" /> and the <see cref="IOutboxReader" /> interfaces.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutbox<TOutbox>(this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TOutbox : class, IOutboxWriter, IOutboxReader
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOutbox<TOutbox, TOutbox>();
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OutboxProduceStrategy" /> using a database
        ///         table as outbox.
        ///     </para>
        ///     <para>
        ///         The <see cref="OutboxProduceStrategy" /> stores the outbound messages into an intermediate outbox,
        ///         participating in the database transaction. The outbound messages become therefore transactional
        ///         with the side effects on the local database.
        ///     </para>
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboxDatabaseTable(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOutbox<DbOutboxWriter, DbOutboxReader>();
        }
    }
}
