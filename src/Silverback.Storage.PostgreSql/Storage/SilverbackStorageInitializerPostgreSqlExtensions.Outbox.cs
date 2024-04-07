// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Storage;

/// <content>
///     Adds the <c>CreatePostgreSqlOutboxAsync</c> methods to the <see cref="SilverbackStorageInitializer" />.
/// </content>
public static partial class SilverbackStorageInitializerPostgreSqlExtensions
{
    /// <summary>
    ///     Creates the PostgreSql outbox table.
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
    public static Task CreatePostgreSqlOutboxAsync(this SilverbackStorageInitializer initializer, string connectionString) =>
        CreatePostgreSqlOutboxAsync(
            initializer,
            new PostgreSqlOutboxSettings(Check.NotNull(connectionString, nameof(connectionString))));

    /// <summary>
    ///     Creates the PostgreSql outbox table.
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
    public static Task CreatePostgreSqlOutboxAsync(this SilverbackStorageInitializer initializer, PostgreSqlOutboxSettings settings)
    {
        Check.NotNull(settings, nameof(settings));
        return CreatePostgreSqlOutboxAsync(initializer, settings.ConnectionString, settings.TableName, settings.CreateTableTimeout);
    }

    /// <summary>
    ///     Creates the PostgreSql outbox table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
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
    public static Task CreatePostgreSqlOutboxAsync(
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
                     "Id SERIAL PRIMARY KEY," +
                     "Content BYTEA," +
                     "Headers TEXT," +
                     "EndpointName TEXT NOT NULL," +
                     "DynamicEndpoint TEXT," +
                     "Created TIMESTAMP WITH TIME ZONE NOT NULL);";

        return dataAccess.ExecuteNonQueryAsync(sql, null, timeout);
    }
}
