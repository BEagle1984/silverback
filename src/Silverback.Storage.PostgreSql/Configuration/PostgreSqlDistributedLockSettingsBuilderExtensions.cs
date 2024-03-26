// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Lock;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="UsePostgreSqlAdvisoryLock" /> and  <see cref="UsePostgreSqlTable" /> methods to the
///     <see cref="DistributedLockSettingsBuilder" />.
/// </summary>
public static class PostgreSqlDistributedLockSettingsBuilderExtensions
{
    /// <summary>
    ///     Configures the PostgreSql advisory lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="DistributedLockSettingsBuilder" />.
    /// </param>
    /// <param name="lockName">
    ///     The lock name.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlAdvisoryLockSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static PostgreSqlAdvisoryLockSettingsBuilder UsePostgreSqlAdvisoryLock(
        this DistributedLockSettingsBuilder builder,
        string lockName,
        string connectionString) =>
        new(lockName, connectionString);

    /// <summary>
    ///     Configures the PostgreSql table based lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="DistributedLockSettingsBuilder" />.
    /// </param>
    /// <param name="lockName">
    ///     The lock name.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlAdvisoryLockSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static PostgreSqlTableLockSettingsBuilder UsePostgreSqlTable(
        this DistributedLockSettingsBuilder builder,
        string lockName,
        string connectionString) =>
        new(lockName, connectionString);
}
