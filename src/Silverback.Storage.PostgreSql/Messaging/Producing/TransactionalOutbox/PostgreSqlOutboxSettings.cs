// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="PostgreSqlOutboxWriter" /> and <see cref="PostgreSqlOutboxReader" /> settings.
/// </summary>
public record PostgreSqlOutboxSettings : OutboxSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlOutboxSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"Silverback_Outbox"</c> will be used.
    /// </param>
    public PostgreSqlOutboxSettings(string connectionString, string? tableName = null)
    {
        ConnectionString = connectionString;
        TableName = tableName ?? "Silverback_Outbox";
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///     Gets the name of the outbox table. The default is <c>"Silverback_Outbox"</c>.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     Returns an instance of <see cref="PostgreSqlLockSettings" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="PostgreSqlLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new PostgreSqlLockSettings($"outbox.{ConnectionString.GetSha256Hash()}.{TableName}", ConnectionString);

    /// <inheritdoc cref="OutboxSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The outbox table name is required.");
    }
}