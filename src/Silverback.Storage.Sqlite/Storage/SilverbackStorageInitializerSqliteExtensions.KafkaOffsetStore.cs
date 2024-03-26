// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Storage;

/// <content>
///     Adds the <c>CreateSqliteKafkaOffsetStoreAsync</c> methods to the <see cref="SilverbackStorageInitializer" />.
/// </content>
public static partial class SilverbackStorageInitializerSqliteExtensions
{
    /// <summary>
    ///     Creates the Sqlite kafka offset store table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteKafkaOffsetStoreAsync(this SilverbackStorageInitializer initializer, string connectionString) =>
        CreateSqliteKafkaOffsetStoreAsync(
            initializer,
            new SqliteKafkaOffsetStoreSettings(Check.NotNull(connectionString, nameof(connectionString))));

    /// <summary>
    ///     Creates the Sqlite kafka offset store table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="settings">
    ///     The kafka offset store settings.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteKafkaOffsetStoreAsync(
        this SilverbackStorageInitializer initializer,
        SqliteKafkaOffsetStoreSettings settings)
    {
        Check.NotNull(settings, nameof(settings));
        return CreateSqliteKafkaOffsetStoreAsync(initializer, settings.ConnectionString, settings.TableName, settings.CreateTableTimeout);
    }

    /// <summary>
    ///     Creates the Sqlite kafka offset store table.
    /// </summary>
    /// <param name="initializer">
    ///     The <see cref="SilverbackStorageInitializer" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the kafka offset store table.
    /// </param>
    /// <param name="timeout">
    ///   The table creation timeout.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task CreateSqliteKafkaOffsetStoreAsync(
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
                     "GroupId TEXT NOT NULL," +
                     "Topic TEXT NOT NULL," +
                     "Partition INTEGER NOT NULL," +
                     "Offset INTEGER NOT NULL," +
                     "PRIMARY KEY (GroupId, Topic, Partition));";

        return dataAccess.ExecuteNonQueryAsync(sql, null, timeout);
    }
}
