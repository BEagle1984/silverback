// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The <see cref="PostgreSqlKafkaOffsetStore" /> settings.
/// </summary>
public record PostgreSqlKafkaOffsetStoreSettings : KafkaOffsetStoreSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlKafkaOffsetStoreSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the kafkaOffsetStore table. If not specified, the default <c>"Silverback_KafkaOffsetStore"</c> will be used.
    /// </param>
    public PostgreSqlKafkaOffsetStoreSettings(string connectionString, string? tableName = null)
    {
        ConnectionString = connectionString;
        TableName = tableName ?? "Silverback_KafkaOffsetStore";
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///     Gets the name of the kafkaOffsetStore table. The default is <c>"Silverback_KafkaOffsetStore"</c>.
    /// </summary>
    public string TableName { get; }

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
