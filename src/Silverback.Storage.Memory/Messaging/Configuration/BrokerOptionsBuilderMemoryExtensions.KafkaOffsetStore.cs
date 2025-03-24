// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddInMemoryKafkaOffsetStore" /> and <see cref="UseInMemoryKafkaOffsetStore" /> methods to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderMemoryExtensions
{
    /// <summary>
    ///     Replaces all offset stores with the in-memory version, better suitable for testing.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder UseInMemoryKafkaOffsetStore(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.AddInMemoryKafkaOffsetStore();

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        factory.OverrideFactories((_, _) => new InMemoryKafkaOffsetStore());

        return builder;
    }

    /// <summary>
    ///     Adds the in-memory offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddInMemoryKafkaOffsetStore(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        if (!factory.HasFactory<InMemoryKafkaOffsetStoreSettings>())
            factory.AddFactory<InMemoryKafkaOffsetStoreSettings>((_, _) => new InMemoryKafkaOffsetStore());

        return builder;
    }
}
