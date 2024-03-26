// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddSqliteKafkaOffsetStore" /> and <see cref="UseSqliteKafkaOffsetStore" /> methods to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderSqliteExtensions
{
    /// <summary>
    ///     Replaces all offset stores with the Sqlite version, better suitable for testing.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder UseSqliteKafkaOffsetStore(this BrokerOptionsBuilder builder, string connectionString)
    {
        Check.NotNull(builder, nameof(builder));

        builder.AddSqliteKafkaOffsetStore();

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        factory.OverrideFactories((settings, _) => new SqliteKafkaOffsetStore(MapSqliteSettings(settings, connectionString)));

        return builder;
    }

    /// <summary>
    ///     Adds the Sqlite offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddSqliteKafkaOffsetStore(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        KafkaOffsetStoreFactory factory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<KafkaOffsetStoreFactory>() ??
                                          throw new InvalidOperationException("OffsetStoreFactory not found, AddKafka has not been called.");

        if (!factory.HasFactory<SqliteKafkaOffsetStoreSettings>())
            factory.AddFactory<SqliteKafkaOffsetStoreSettings>((settings, _) => new SqliteKafkaOffsetStore(settings));

        return builder;
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Not security relevant")]
    private static SqliteKafkaOffsetStoreSettings MapSqliteSettings(KafkaOffsetStoreSettings settings, string connectionString)
    {
        if (settings is SqliteKafkaOffsetStoreSettings sqliteSettings)
            return sqliteSettings;

#if NET7_0_OR_GREATER
        string settingsHash = BitConverter.ToString(MD5.HashData(JsonSerializer.SerializeToUtf8Bytes(settings, settings.GetType())));
#else
        using MD5 md5 = MD5.Create();
        string settingsHash = BitConverter.ToString(md5.ComputeHash(JsonSerializer.SerializeToUtf8Bytes(settings, settings.GetType())));
#endif

        sqliteSettings = new SqliteKafkaOffsetStoreSettings(connectionString)
        {
            TableName = settingsHash,
        };

        if (settings is IDatabaseConnectionSettings databaseSettings)
        {
            sqliteSettings = sqliteSettings with
            {
                DbCommandTimeout = databaseSettings.DbCommandTimeout,
                CreateTableTimeout = databaseSettings.CreateTableTimeout
            };
        }

        return sqliteSettings;
    }
}
