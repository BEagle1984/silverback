// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <summary>
///     The <see cref="PostgreSqlAdvisoryLock" /> settings.
/// </summary>
public record PostgreSqlAdvisoryLockSettings : DistributedLockSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlAdvisoryLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlAdvisoryLockSettings(string lockName, string connectionString)
        : base(lockName)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the connection string to the PostgreSql database.
    /// </summary>
    public string ConnectionString { get; }

    /// <inheritdoc cref="DistributedLockSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");
    }
}
