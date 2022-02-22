// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The <see cref="SqliteOutboxWriter" /> and <see cref="SqliteOutboxReader"/> settings.
/// </summary>
public record SqliteOutboxSettings : OutboxSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettings" /> class.
    /// </summary>
    public SqliteOutboxSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///    The connection string to the SQLite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"SilverbackOutbox"</c> will be used.
    /// </param>
    public SqliteOutboxSettings(string connectionString, string? tableName = null)
    {
        ConnectionString = Check.NotNullOrEmpty(connectionString, nameof(connectionString));

        if (tableName != null)
            TableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
    }

    /// <summary>
    ///     Gets the connection string to the SQLite database.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the outbox table. The default is <c>"SilverbackOutbox"</c>.
    /// </summary>
    public string TableName { get; init; } = "SilverbackOutbox";

    /// <summary>
    ///     Returns an instance of <see cref="InMemoryLockSettings" />, since there is no distributed lock implementation for Sqlite and
    ///     the in-memory lock is enough for testing.
    /// </summary>
    /// <returns>
    ///     The <see cref="InMemoryLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new InMemoryLockSettings($"outbox.{ConnectionString}.{TableName}");
}
