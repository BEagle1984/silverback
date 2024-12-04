// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="PostgreSqlKafkaOffsetStoreSettings" />.
/// </summary>
public class PostgreSqlKafkaOffsetStoreSettingsBuilder : IKafkaOffsetStoreSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private string? _tableName;

    private TimeSpan? _dbCommandTimeout;

    private TimeSpan? _createTableTimeout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlKafkaOffsetStoreSettingsBuilder" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlKafkaOffsetStoreSettingsBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Sets the table name.
    /// </summary>
    /// <param name="tableName">
    ///     The name of the Kafka offset store table. If not specified, the default <c>"SilverbackKafkaOffsets"</c> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlKafkaOffsetStoreSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlKafkaOffsetStoreSettingsBuilder UseTable(string tableName)
    {
        _tableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
        return this;
    }

    /// <summary>
    ///     Sets the database command timeout.
    /// </summary>
    /// <param name="dbCommandTimeout">
    ///     The timeout for the database commands. The default is 10 seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlKafkaOffsetStoreSettingsBuilder WithDbCommandTimeout(TimeSpan dbCommandTimeout)
    {
        if (dbCommandTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(dbCommandTimeout), dbCommandTimeout, "The timeout must be greater than zero.");

        _dbCommandTimeout = dbCommandTimeout;
        return this;
    }

    /// <summary>
    ///     Sets the timeout for the table creation.
    /// </summary>
    /// <param name="createTableTimeout">
    ///     The timeout for the table creation. The default is 30 seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlKafkaOffsetStoreSettingsBuilder WithCreateTableTimeout(TimeSpan createTableTimeout)
    {
        if (createTableTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(createTableTimeout), createTableTimeout, "The timeout must be greater than zero.");

        _createTableTimeout = createTableTimeout;
        return this;
    }

    /// <inheritdoc cref="IKafkaOffsetStoreSettingsImplementationBuilder.Build" />
    public KafkaOffsetStoreSettings Build()
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new(_connectionString);

        settings = settings with
        {
            TableName = _tableName ?? settings.TableName,
            DbCommandTimeout = _dbCommandTimeout ?? settings.DbCommandTimeout,
            CreateTableTimeout = _createTableTimeout ?? settings.CreateTableTimeout
        };

        settings.Validate();

        return settings;
    }
}
