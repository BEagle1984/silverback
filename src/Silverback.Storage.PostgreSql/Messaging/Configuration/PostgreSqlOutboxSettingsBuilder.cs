// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="PostgreSqlOutboxSettings" />.
/// </summary>
public class PostgreSqlOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private string? _tableName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlOutboxSettingsBuilder" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the PostgreSql database.
    /// </param>
    public PostgreSqlOutboxSettingsBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Sets the table name.
    /// </summary>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"SilverbackOutbox"</c> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="PostgreSqlOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Review method name (With...)
    public PostgreSqlOutboxSettingsBuilder WithTableName(string tableName)
    {
        _tableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
        return this;
    }

    /// <inheritdoc cref="IOutboxSettingsImplementationBuilder.Build" />
    public OutboxSettings Build()
    {
        PostgreSqlOutboxSettings settings = new(_connectionString, _tableName);

        settings.Validate();

        return settings;
    }
}
