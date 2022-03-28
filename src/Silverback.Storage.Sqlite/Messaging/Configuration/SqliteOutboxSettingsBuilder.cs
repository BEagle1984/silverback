// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="SqliteOutboxSettings" />.
/// </summary>
public class SqliteOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private string? _tableName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettingsBuilder" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    public SqliteOutboxSettingsBuilder(string connectionString)
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
    ///     The <see cref="InMemoryOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Review method name (With...)
    public SqliteOutboxSettingsBuilder WithTableName(string tableName)
    {
        _tableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
        return this;
    }

    /// <inheritdoc cref="IOutboxSettingsImplementationBuilder.Build" />
    public OutboxSettings Build()
    {
        SqliteOutboxSettings settings = new(_connectionString);

        if (_tableName != null)
            settings = settings with { TableName = _tableName };

        settings.Validate();

        return settings;
    }
}
