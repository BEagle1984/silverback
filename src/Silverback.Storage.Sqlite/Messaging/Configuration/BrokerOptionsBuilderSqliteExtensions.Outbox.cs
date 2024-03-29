// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <see cref="AddSqliteOutbox" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public static partial class BrokerOptionsBuilderSqliteExtensions
{
    /// <summary>
    ///     Replaces all outboxes with the SQLite version, better suitable for testing.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
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

        readerFactory.OverrideFactories((settings, _) => new SqliteOutboxReader(MapSqliteSettings(settings, connectionString)));
        writerFactory.OverrideFactories((settings, _) => new SqliteOutboxWriter(MapSqliteSettings(settings, connectionString)));

        return builder;
    }

    /// <summary>
    ///     Adds the SQLite outbox.
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
            readerFactory.AddFactory<SqliteOutboxSettings>((settings, _) => new SqliteOutboxReader(settings));

        if (!writerFactory.HasFactory<SqliteOutboxSettings>())
            writerFactory.AddFactory<SqliteOutboxSettings>((settings, _) => new SqliteOutboxWriter(settings));

        builder.SilverbackBuilder.AddInMemoryLock();

        return builder;
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Not security relevant")]
    private static SqliteOutboxSettings MapSqliteSettings(OutboxSettings settings, string connectionString)
    {
        if (settings is SqliteOutboxSettings sqliteSettings)
            return sqliteSettings;

#if NET7_0_OR_GREATER
        string settingsHash = BitConverter.ToString(MD5.HashData(JsonSerializer.SerializeToUtf8Bytes(settings, settings.GetType())));
#else
        using MD5 md5 = MD5.Create();
        string settingsHash = BitConverter.ToString(md5.ComputeHash(JsonSerializer.SerializeToUtf8Bytes(settings, settings.GetType())));
#endif

        sqliteSettings = new SqliteOutboxSettings(connectionString)
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
