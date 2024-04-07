// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Storage;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The <see cref="SqliteKafkaOffsetStore" /> settings.
/// </summary>
public record SqliteKafkaOffsetStoreSettings : KafkaOffsetStoreSettings, IDatabaseConnectionSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStoreSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    public SqliteKafkaOffsetStoreSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the SQLite database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///     Gets The name of the Kafka offset store table. The default is <c>"SilverbackKafkaOffsets"</c>.
    /// </summary>
    public string TableName { get; init; } = "SilverbackKafkaOffsets";

    /// <summary>
    ///     Gets the database command timeout. The default is 10 seconds.
    /// </summary>
    public TimeSpan DbCommandTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Gets the timeout for the table creation. The default is 30 seconds.
    /// </summary>
    public TimeSpan CreateTableTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc cref="KafkaOffsetStoreSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The KafkaOffsetStore table name is required.");

        if (DbCommandTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The command timeout must be greater than zero.");

        if (CreateTableTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The create table timeout must be greater than zero.");
    }
}
