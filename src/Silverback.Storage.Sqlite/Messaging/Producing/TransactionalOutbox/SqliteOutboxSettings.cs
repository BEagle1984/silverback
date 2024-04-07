// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Lock;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="SqliteOutboxWriter" /> and <see cref="SqliteOutboxReader" /> settings.
/// </summary>
public record SqliteOutboxSettings : OutboxSettings, IDatabaseConnectionSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    public SqliteOutboxSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the SQLite database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///     Gets the name of the outbox table. The default is <c>"SilverbackOutbox"</c>.
    /// </summary>
    public string TableName { get; init; } = "SilverbackOutbox";

    /// <summary>
    ///     Gets the database command timeout. The default is 10 seconds.
    /// </summary>
    public TimeSpan DbCommandTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Gets the timeout for the table creation. The default is 30 seconds.
    /// </summary>
    public TimeSpan CreateTableTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Returns an instance of <see cref="InMemoryLockSettings" />, since there is no distributed lock implementation for Sqlite and
    ///     the in-memory lock is enough for testing.
    /// </summary>
    /// <returns>
    ///     The <see cref="InMemoryLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new InMemoryLockSettings($"outbox.{ConnectionString.GetSha256Hash()}.{TableName}");

    /// <inheritdoc cref="OutboxSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The outbox table name is required.");

        if (DbCommandTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The command timeout must be greater than zero.");

        if (CreateTableTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The create table timeout must be greater than zero.");
    }
}
