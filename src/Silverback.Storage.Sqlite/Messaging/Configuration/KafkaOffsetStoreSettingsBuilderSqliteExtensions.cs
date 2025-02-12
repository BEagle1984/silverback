// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseSqlite" /> method to the <see cref="KafkaOffsetStoreSettingsBuilder" />.
/// </summary>
public static class KafkaOffsetStoreSettingsBuilderSqliteExtensions
{
    /// <summary>
    ///     Configures the SQLite offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="KafkaOffsetStoreSettingsBuilder" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteKafkaOffsetStoreSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static SqliteKafkaOffsetStoreSettingsBuilder UseSqlite(this KafkaOffsetStoreSettingsBuilder builder, string connectionString) =>
        new(connectionString);
}
