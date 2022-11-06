// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The <see cref="InMemoryKafkaOffsetStore" /> settings.
/// </summary>
public record SqliteKafkaOffsetStoreSettings : KafkaOffsetStoreSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStoreSettings" /> class.
    /// </summary>
    public SqliteKafkaOffsetStoreSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStoreSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the kafkaOffsetStore table. If not specified, the default <c>"SilverbackKafkaOffsetStore"</c> will be used.
    /// </param>
    public SqliteKafkaOffsetStoreSettings(string connectionString, string? tableName = null)
    {
        ConnectionString = connectionString;

        if (tableName != null)
            TableName = tableName;
    }

    /// <summary>
    ///     Gets the connection string to the Sqlite database.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the kafkaOffsetStore table. The default is <c>"SilverbackKafkaOffsetStore"</c>.
    /// </summary>
    public string TableName { get; init; } = "Silverback_KafkaOffsetStore";

    /// <inheritdoc cref="KafkaOffsetStoreSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The kafkaOffsetStore table name is required.");
    }
}
