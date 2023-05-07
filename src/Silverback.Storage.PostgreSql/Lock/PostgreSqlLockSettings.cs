// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <summary>
///     The <see cref="PostgreSqlLock" /> settings.
/// </summary>
public record PostgreSqlLockSettings : DistributedLockSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlLockSettings(string lockName, string connectionString)
        : base(lockName)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
    /// </summary>
    public string ConnectionString { get; }
}
