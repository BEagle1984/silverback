// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Producing.TransactionalOutbox;
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

        InMemoryOutboxFactory outboxFactory = GetInMemoryOutboxFactory(builder);

        readerFactory.OverrideFactories((settings, _) => new InMemoryOutboxReader(outboxFactory.GetOutbox(settings)));
        writerFactory.OverrideFactories(
            (settings, serviceProvider) => new InMemoryOutboxWriter(
                outboxFactory.GetOutbox(settings),
                serviceProvider.GetRequiredService<ISilverbackLogger<InMemoryOutboxWriter>>()));

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

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        InMemoryOutboxFactory outboxFactory = GetInMemoryOutboxFactory(builder);

        if (!readerFactory.HasFactory<InMemoryOutboxSettings>())
            readerFactory.AddFactory<InMemoryOutboxSettings>((settings, _) => new InMemoryOutboxReader(outboxFactory.GetOutbox(settings)));

        if (!writerFactory.HasFactory<InMemoryOutboxSettings>())
        {
            writerFactory.AddFactory<InMemoryOutboxSettings>(
                (settings, serviceProvider) => new InMemoryOutboxWriter(
                    outboxFactory.GetOutbox(settings),
                    serviceProvider.GetRequiredService<ISilverbackLogger<InMemoryOutboxWriter>>()));
        }

        builder.SilverbackBuilder.AddInMemoryLock();

        return builder;
    }

    private static InMemoryOutboxFactory GetInMemoryOutboxFactory(BrokerOptionsBuilder builder)
    {
        InMemoryOutboxFactory? outboxFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<InMemoryOutboxFactory>();

        if (outboxFactory == null)
        {
            outboxFactory = new InMemoryOutboxFactory();
            builder.SilverbackBuilder.Services.AddSingleton(outboxFactory);
        }

        return outboxFactory;
    }
}
