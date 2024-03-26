// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Lock;

/// <summary>
///     Builds the <see cref="PostgreSqlAdvisoryLockSettings" />.
/// </summary>
public class PostgreSqlAdvisoryLockSettingsBuilder : IDistributedLockSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private readonly string _lockName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlAdvisoryLockSettingsBuilder" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlAdvisoryLockSettingsBuilder(string lockName, string connectionString)
    {
        _lockName = lockName;
        _connectionString = connectionString;
    }

    /// <inheritdoc cref="IDistributedLockSettingsImplementationBuilder.Build" />
    public DistributedLockSettings Build()
    {
        PostgreSqlAdvisoryLockSettings settings = new(_lockName, _connectionString);

        settings.Validate();

        return settings;
    }
}
