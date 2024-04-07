// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Lock;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Storage;

/// <content>
///     Adds the <c>CreatePostgreSqlLocksAsync</c> methods to the <see cref="SilverbackStorageInitializer" />.
/// </content>
public static partial class SilverbackStorageInitializerPostgreSqlExtensions
{
    /// <summary>
    ///     Creates the PostgreSql locks table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreatePostgreSqlLocksTableAsync(this SilverbackStorageInitializer initializer, string connectionString) =>
        CreatePostgreSqlLocksTableAsync(
            initializer,
            new PostgreSqlTableLockSettings("lock", Check.NotNull(connectionString, nameof(connectionString))));

    /// <summary>
    ///     Creates the PostgreSql locks table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="settings">
    ///     The locks settings.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreatePostgreSqlLocksTableAsync(this SilverbackStorageInitializer initializer, PostgreSqlTableLockSettings settings)
    {
        Check.NotNull(settings, nameof(settings));
        return CreatePostgreSqlLocksTableAsync(initializer, settings.ConnectionString, settings.TableName, settings.CreateTableTimeout);
    }

    /// <summary>
    ///     Creates the PostgreSql locks table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the locks table.
    /// </param>
    /// <param name="timeout">
    ///     The table creation timeout.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreatePostgreSqlLocksTableAsync(
        this SilverbackStorageInitializer initializer,
        string connectionString,
        string tableName,
        TimeSpan timeout)
    {
        Check.NotNull(initializer, nameof(initializer));
        Check.NotNullOrEmpty(connectionString, nameof(connectionString));
        Check.NotNullOrEmpty(tableName, nameof(tableName));

        PostgreSqlDataAccess dataAccess = new(connectionString);

        string sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (" +
                     "LockName TEXT PRIMARY KEY," +
                     "Handler TEXT," +
                     "AcquiredOn TIMESTAMP," +
                     "LastHeartbeat TIMESTAMP" +
                     ")";

        return dataAccess.ExecuteNonQueryAsync(sql, null, timeout);
    }
}
