// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddPostgreSqlKafkaOffsetStore" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderPostgreSqlExtensions
{
    /// <summary>
    ///     Adds the PostgreSql offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddPostgreSqlKafkaOffsetStore(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        if (!factory.HasFactory<PostgreSqlKafkaOffsetStoreSettings>())
            factory.AddFactory<PostgreSqlKafkaOffsetStoreSettings>(settings => new PostgreSqlKafkaOffsetStore(settings));

        return builder;
    }
}
