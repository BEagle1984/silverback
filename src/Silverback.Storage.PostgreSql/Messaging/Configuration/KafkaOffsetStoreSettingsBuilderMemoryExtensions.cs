// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UsePostgreSql" /> method to the <see cref="KafkaOffsetStoreSettingsBuilder" />.
/// </summary>
public static class KafkaOffsetStoreSettingsBuilderMemoryExtensions
{
    /// <summary>
    ///     Configures the offsetStore to be stored in memory.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="KafkaOffsetStoreSettingsBuilder" />.
    /// </param>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlKafkaOffsetStoreSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static PostgreSqlKafkaOffsetStoreSettingsBuilder UsePostgreSql(this KafkaOffsetStoreSettingsBuilder builder, string connectionString) =>
        new(connectionString);
}
