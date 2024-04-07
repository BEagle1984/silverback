// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Configuration;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     Builds the <see cref="PostgreSqlTableLockSettings" />.
/// </summary>
public class PostgreSqlTableLockSettingsBuilder : IDistributedLockSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private readonly string _lockName;

    private string? _tableName;

    private TimeSpan? _dbCommandTimeout;

    private TimeSpan? _createTableTimeout;

    private TimeSpan? _acquireInterval;

    private TimeSpan? _heartbeatInterval;

    private TimeSpan? _lockTimeout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlTableLockSettingsBuilder" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlTableLockSettingsBuilder(string lockName, string connectionString)
    {
        _lockName = lockName;
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Sets the table name.
    /// </summary>
    /// <param name="tableName">
    ///     The name of the locks table. If not specified, the default <c>"SilverbackLocks"</c> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Review method name (With...)
    public PostgreSqlTableLockSettingsBuilder WithTableName(string tableName)
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
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlTableLockSettingsBuilder WithDbCommandTimeout(TimeSpan dbCommandTimeout)
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
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlTableLockSettingsBuilder WithCreateTableTimeout(TimeSpan createTableTimeout)
    {
        if (createTableTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(createTableTimeout), createTableTimeout, "The timeout must be greater than zero.");

        _createTableTimeout = createTableTimeout;
        return this;
    }

    /// <summary>
    ///     Sets the interval between two attempts to acquire the lock.
    /// </summary>
    /// <param name="acquireInterval">
    ///     The interval between two attempts to acquire the lock. The default is 1 second.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlTableLockSettingsBuilder WithAcquireInterval(TimeSpan acquireInterval)
    {
        if (acquireInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(acquireInterval), acquireInterval, "The interval must be greater than zero.");

        _acquireInterval = acquireInterval;
        return this;
    }

    /// <summary>
    ///     Sets the interval between two heartbeat updates.
    /// </summary>
    /// <param name="heartbeatInterval">
    ///     The interval between two heartbeat updates. The default is 500 milliseconds.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlTableLockSettingsBuilder WithHeartbeatInterval(TimeSpan heartbeatInterval)
    {
        if (heartbeatInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(heartbeatInterval), heartbeatInterval, "The interval must be greater than zero.");

        _heartbeatInterval = heartbeatInterval;
        return this;
    }

    /// <summary>
    ///     Sets the maximum duration between two heartbeat updates, after which the lock is considered lost.
    /// </summary>
    /// <param name="lockTimeout">
    ///     The maximum duration between two heartbeat updates, after which the lock is considered lost. The default is 5 seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlTableLockSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public PostgreSqlTableLockSettingsBuilder WithLockTimeout(TimeSpan lockTimeout)
    {
        if (lockTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lockTimeout), lockTimeout, "The timeout must be greater than zero.");

        _lockTimeout = lockTimeout;
        return this;
    }

    /// <inheritdoc cref="IDistributedLockSettingsImplementationBuilder.Build" />
    public DistributedLockSettings Build()
    {
        PostgreSqlTableLockSettings settings = new(_lockName, _connectionString);

        settings = settings with
        {
            TableName = _tableName ?? settings.TableName,
            DbCommandTimeout = _dbCommandTimeout ?? settings.DbCommandTimeout,
            CreateTableTimeout = _createTableTimeout ?? settings.CreateTableTimeout,
            AcquireInterval = _acquireInterval ?? settings.AcquireInterval,
            HeartbeatInterval = _heartbeatInterval ?? settings.HeartbeatInterval,
            LockTimeout = _lockTimeout ?? settings.LockTimeout
        };

        settings.Validate();

        return settings;
    }
}
