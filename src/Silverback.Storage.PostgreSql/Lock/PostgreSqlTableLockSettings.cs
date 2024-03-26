// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Storage;

namespace Silverback.Lock;

/// <summary>
///     The <see cref="PostgreSqlTableLock" /> settings.
/// </summary>
public record PostgreSqlTableLockSettings : DistributedLockSettings, IDatabaseConnectionSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlTableLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlTableLockSettings(string lockName, string connectionString)
        : base(lockName)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///     Gets the name of the locks table. The default is <c>"SilverbackLocks"</c>.
    /// </summary>
    public string TableName { get; init; } = "SilverbackLocks";

    /// <summary>
    ///     Gets the database command timeout. The default is 10 seconds.
    /// </summary>
    public TimeSpan DbCommandTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Gets the timeout for the table creation. The default is 30 seconds.
    /// </summary>
    public TimeSpan CreateTableTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///   Gets the interval between two attempts to acquire the lock. The default is 1 second.
    /// </summary>
    public TimeSpan AcquireInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///  Gets the interval between two heartbeat updates. The default is 500 milliseconds.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///  Gets the maximum duration between two heartbeat updates, after which the lock is considered lost. The default is 5 seconds.
    /// </summary>
    public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="DistributedLockSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The lock table name is required.");

        if (DbCommandTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The command timeout must be greater than zero.");

        if (CreateTableTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The create table timeout must be greater than zero.");

        if (AcquireInterval <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The acquire interval must be greater than zero.");

        if (HeartbeatInterval <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The heartbeat interval must be greater than zero.");

        if (LockTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The lock timeout must be greater than zero.");

        if (LockTimeout <= HeartbeatInterval)
            throw new SilverbackConfigurationException("The lock timeout must be greater than the heartbeat interval.");
    }
}
