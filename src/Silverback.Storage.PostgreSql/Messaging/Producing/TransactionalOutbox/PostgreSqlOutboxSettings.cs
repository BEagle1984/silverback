// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Lock;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="PostgreSqlOutboxWriter" /> and <see cref="PostgreSqlOutboxReader" /> settings.
/// </summary>
public record PostgreSqlOutboxSettings : OutboxSettings, IDatabaseConnectionSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlOutboxSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlOutboxSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
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
    ///     Returns an instance of <see cref="PostgreSqlAdvisoryLockSettings" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="PostgreSqlAdvisoryLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new PostgreSqlAdvisoryLockSettings($"outbox.{ConnectionString.GetSha256Hash()}.{TableName}", ConnectionString);

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
