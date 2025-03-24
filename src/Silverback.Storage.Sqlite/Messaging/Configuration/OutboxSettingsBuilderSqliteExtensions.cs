// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseSqlite" /> method to the <see cref="OutboxSettingsBuilder" />.
/// </summary>
public static class OutboxSettingsBuilderSqliteExtensions
{
    /// <summary>
    ///     Configures the SQLite outbox.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="OutboxSettingsBuilder" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteOutboxSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static SqliteOutboxSettingsBuilder UseSqlite(this OutboxSettingsBuilder builder, string connectionString) =>
        new(connectionString);
}
