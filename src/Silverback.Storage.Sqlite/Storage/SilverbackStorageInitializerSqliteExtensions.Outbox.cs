// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Storage;

/// <content>
///     Adds the <c>CreateSqliteOutboxAsync</c> methods to the <see cref="SilverbackStorageInitializer" />.
/// </content>
public static partial class SilverbackStorageInitializerSqliteExtensions
{
    /// <summary>
    ///     Creates the Sqlite outbox table.
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
        return CreateSqliteOutboxAsync(initializer, settings.ConnectionString, settings.TableName);
    }

    /// <summary>
    ///     Creates the Sqlite outbox table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"SilverbackOutbox"</c> will be used.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteOutboxAsync(
        this SilverbackStorageInitializer initializer,
        string connectionString,
        string tableName)
    {
        Check.NotNull(initializer, nameof(initializer));
        Check.NotNullOrEmpty(connectionString, nameof(connectionString));
        Check.NotNullOrEmpty(tableName, nameof(tableName));

        SqliteDataAccess dataAccess = new(connectionString);

        // TODO: No transaction possibility?
        return dataAccess.ExecuteNonQueryAsync(
            $"CREATE TABLE IF NOT EXISTS {tableName} (" +
            "Id INTEGER PRIMARY KEY ," +
            "MessageType TEXT NOT NULL," +
            "Content BLOB," +
            "Headers TEXT," +
            "EndpointRawName TEXT NOT NULL," +
            "EndpointFriendlyName TEXT," +
            "SerializedEndpoint TEXT," +
            "Created INTEGER)");
    }
}
