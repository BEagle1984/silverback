// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Storage;

/// <content>
///     Adds the <c>CreateSqliteOutboxAsync</c> methods to the <see cref="SilverbackStorageInitializer" />.
/// </content>
public static partial class SilverbackStorageInitializerSqliteExtensions
{
    /// <summary>
    ///     Creates the SQLite outbox table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteOutboxAsync(this SilverbackStorageInitializer initializer, string connectionString) =>
        CreateSqliteOutboxAsync(
            initializer,
            new SqliteOutboxSettings(Check.NotNull(connectionString, nameof(connectionString))));

    /// <summary>
    ///     Creates the SQLite outbox table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteOutboxAsync(
        this SilverbackStorageInitializer initializer,
        SqliteOutboxSettings settings)
    {
        Check.NotNull(settings, nameof(settings));
        return CreateSqliteOutboxAsync(initializer, settings.ConnectionString, settings.TableName, settings.CreateTableTimeout);
    }

    /// <summary>
    ///     Creates the SQLite outbox table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the outbox table.
    /// </param>
    /// <param name="timeout">
    ///   The table creation timeout.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteOutboxAsync(
        this SilverbackStorageInitializer initializer,
        string connectionString,
        string tableName,
        TimeSpan timeout)
    {
        Check.NotNull(initializer, nameof(initializer));
        Check.NotNullOrEmpty(connectionString, nameof(connectionString));
        Check.NotNullOrEmpty(tableName, nameof(tableName));

        SqliteDataAccess dataAccess = new(connectionString);

        string sql = $"CREATE TABLE IF NOT EXISTS {tableName} (" +
                     "Id INTEGER NOT NULL," +
                     "Content BLOB," +
                     "Headers TEXT," +
                     "EndpointName TEXT NOT NULL," +
                     "Created INTEGER NOT NULL," +
                     "PRIMARY KEY (Id));";

        return dataAccess.ExecuteNonQueryAsync(sql, null, timeout);
    }
}
