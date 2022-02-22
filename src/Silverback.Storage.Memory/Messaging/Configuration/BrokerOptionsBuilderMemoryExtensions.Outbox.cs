// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddInMemoryOutbox" /> and <see cref="UseInMemoryOutbox" /> methods to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderMemoryExtensions
{
    /// <summary>
    ///     Replaces all outboxes with the in-memory version, better suitable for testing.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder UseInMemoryOutbox(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.AddInMemoryOutbox();

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        InMemoryStorageFactory? storageFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<InMemoryStorageFactory>();

        if (storageFactory == null)
            throw new InvalidOperationException("InMemoryStorageFactory not found, AddInMemoryStorage has not been called.");

        readerFactory.OverrideFactories(
            settings =>
            {
                InMemoryStorage<OutboxMessage> inMemoryStorage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(settings);
                return new InMemoryOutboxReader(inMemoryStorage);
            });
        writerFactory.OverrideFactories(
            settings =>
            {
                InMemoryStorage<OutboxMessage> inMemoryStorage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(settings);
                return new InMemoryOutboxWriter(inMemoryStorage);
            });

        return builder;
    }

    /// <summary>
    ///     Adds the in-memory outbox.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddInMemoryOutbox(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.SilverbackBuilder.AddInMemoryStorage();

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        InMemoryStorageFactory? storageFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<InMemoryStorageFactory>();

        if (storageFactory == null)
            throw new InvalidOperationException("InMemoryStorageFactory not found, AddInMemoryStorage has not been called.");

        if (!readerFactory.HasFactory<InMemoryOutboxSettings>())
        {
            readerFactory.AddFactory<InMemoryOutboxSettings>(
                settings =>
                {
                    InMemoryStorage<OutboxMessage> inMemoryStorage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(settings);
                    return new InMemoryOutboxReader(inMemoryStorage);
                });
        }

        if (!writerFactory.HasFactory<InMemoryOutboxSettings>())
        {
            writerFactory.AddFactory<InMemoryOutboxSettings>(
                settings =>
                {
                    InMemoryStorage<OutboxMessage> inMemoryStorage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(settings);
                    return new InMemoryOutboxWriter(inMemoryStorage);
                });
        }

        builder.SilverbackBuilder.AddInMemoryLock();

        return builder;
    }
}
