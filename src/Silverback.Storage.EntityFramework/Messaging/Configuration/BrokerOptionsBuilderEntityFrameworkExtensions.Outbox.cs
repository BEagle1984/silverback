// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddEntityFrameworkOutbox" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Adds the Entity Framework based outbox.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddEntityFrameworkOutbox(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!readerFactory.HasFactory<EntityFrameworkOutboxSettings>())
        {
            readerFactory.AddFactory<EntityFrameworkOutboxSettings>(
                (settings, serviceProvider) => new EntityFrameworkOutboxReader(
                    settings,
                    serviceProvider.GetRequiredService<IServiceScopeFactory>()));
        }

        if (!writerFactory.HasFactory<EntityFrameworkOutboxSettings>())
        {
            writerFactory.AddFactory<EntityFrameworkOutboxSettings>(
                (settings, serviceProvider) => new EntityFrameworkOutboxWriter(
                    settings,
                    serviceProvider.GetRequiredService<IServiceScopeFactory>()));
        }

        builder.SilverbackBuilder.AddEntityFrameworkLock();

        return builder;
    }
}
