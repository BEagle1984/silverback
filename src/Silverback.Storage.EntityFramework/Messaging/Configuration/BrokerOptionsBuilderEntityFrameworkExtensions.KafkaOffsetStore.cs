// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddEntityFrameworkKafkaOffsetStore" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Adds the Entity Framework based offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddEntityFrameworkKafkaOffsetStore(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        if (!factory.HasFactory<EntityFrameworkKafkaOffsetStoreSettings>())
        {
            factory.AddFactory<EntityFrameworkKafkaOffsetStoreSettings>(
                (settings, serviceProvider) =>
                    new EntityFrameworkKafkaOffsetStore(settings, serviceProvider.GetRequiredService<IServiceScopeFactory>()));
        }

        return builder;
    }
}
