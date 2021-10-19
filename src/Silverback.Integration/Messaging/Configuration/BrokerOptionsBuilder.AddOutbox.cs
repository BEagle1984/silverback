// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the AddOutbox methods to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public sealed partial class BrokerOptionsBuilder
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
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddOutbox<TOutboxWriter, TOutboxReader>()
        where TOutboxWriter : class, IOutboxWriter
        where TOutboxReader : class, IOutboxReader
    {
        SilverbackBuilder
            .AddScopedSubscriber<OutboxTransactionManager>()
            .Services
            .AddScoped<IOutboxWriter, TOutboxWriter>()
            .AddScoped<IOutboxReader, TOutboxReader>()
            .AddScoped<TransactionalOutboxBroker>();

        return this;
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
    ///     The type implementing both the <see cref="IOutboxWriter" /> and the <see cref="IOutboxReader" />
    ///     interfaces.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddOutbox<TOutbox>()
        where TOutbox : class, IOutboxWriter, IOutboxReader =>
        AddOutbox<TOutbox, TOutbox>();

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
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddOutboxDatabaseTable() => AddOutbox<DbOutboxWriter, DbOutboxReader>();
}
