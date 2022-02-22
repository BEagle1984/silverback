// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddSqliteOutbox" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderSqliteExtensions
{
    /// <summary>
    ///     Replaces all outboxes with the Sqlite version, better suitable for testing.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="connectionString">
    ///    The connection string to the SQLite database.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder UseSqliteOutbox(this BrokerOptionsBuilder builder, string connectionString)
    {
        Check.NotNull(builder, nameof(builder));

        builder.AddSqliteOutbox();

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        // TODO: Map settings to SqliteOutboxSettings: connection string from parameter / outbox name derived from actual settings (hash?)
        // readerFactory.OverrideFactories(settings => new SqliteOutboxReader(settings));
        // writerFactory.AddFactory<SqliteOutboxSettings>(settings => new SqliteOutboxWriter(settings));

        throw new NotImplementedException();

        return builder;
    }

    /// <summary>
    ///     Adds the Sqlite outbox.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddSqliteOutbox(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!readerFactory.HasFactory<SqliteOutboxSettings>())
            readerFactory.AddFactory<SqliteOutboxSettings>(settings => new SqliteOutboxReader(settings));

        if (!writerFactory.HasFactory<SqliteOutboxSettings>())
            writerFactory.AddFactory<SqliteOutboxSettings>(settings => new SqliteOutboxWriter(settings));

        builder.SilverbackBuilder.AddInMemoryLock();

        return builder;
    }
}
