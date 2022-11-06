// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseSqlite" /> method to the <see cref="OutboxSettingsBuilder" />.
/// </summary>
public static class OutboxSettingsBuilderSqliteExtensions
{
    /// <summary>
    ///     Configures the outbox to be stored in memory.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="OutboxSettingsBuilder" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteOutboxSettingsBuilder" />.
    /// </returns>
    public static SqliteOutboxSettingsBuilder UseSqlite(this OutboxSettingsBuilder builder, string connectionString) =>
        new(connectionString);
}
