// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="SqliteKafkaOffsetStoreSettings" />.
/// </summary>
public class SqliteKafkaOffsetStoreSettingsBuilder : IKafkaOffsetStoreSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private string? _tableName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStoreSettingsBuilder" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    public SqliteKafkaOffsetStoreSettingsBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Sets the table name.
    /// </summary>
    /// <param name="tableName">
    ///     The name of the kafkaOffsetStore table. If not specified, the default <c>"SilverbackKafkaOffsetStore"</c> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryKafkaOffsetStoreSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Review method name (With...)
    public SqliteKafkaOffsetStoreSettingsBuilder WithTableName(string tableName)
    {
        _tableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
        return this;
    }

    /// <inheritdoc cref="IKafkaOffsetStoreSettingsImplementationBuilder.Build" />
    public KafkaOffsetStoreSettings Build()
    {
        SqliteKafkaOffsetStoreSettings settings = new(_connectionString);

        if (_tableName != null)
            settings = settings with { TableName = _tableName };

        settings.Validate();

        return settings;
    }
}
