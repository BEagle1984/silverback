// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the SQLite outbox.
/// </summary>
public class SqliteOutboxWriter : IOutboxWriter
{
    private readonly SqliteOutboxSettings _settings;

    private readonly SqliteDataAccess _dataAccess;

    private readonly string _insertSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxWriter" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public SqliteOutboxWriter(SqliteOutboxSettings settings)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new SqliteDataAccess(settings.ConnectionString);

        _insertSql = $"INSERT INTO {settings.TableName} (" +
                     "Content," +
                     "Headers," +
                     "EndpointName," +
                     "DynamicEndpoint," +
                     "Created" +
                     ") VALUES (" +
                     "@Content," +
                     "@Headers," +
                     "@EndpointName," +
                     "@DynamicEndpoint," +
                     "@Created)";
    }

    /// <inheritdoc cref="AddAsync(OutboxMessage, ISilverbackContext)" />
    public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null)
    {
        Check.NotNull(outboxMessage, nameof(outboxMessage));

        return _dataAccess.ExecuteNonQueryAsync(
            _insertSql,
            [
                new SqliteParameter("@Content", SqliteType.Blob)
                {
                    Value = outboxMessage.Content
                },
                new SqliteParameter("@Headers", SqliteType.Text)
                {
                    Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers)
                },
                new SqliteParameter("@EndpointName", SqliteType.Text)
                {
                    Value = outboxMessage.Endpoint.FriendlyName
                },
                new SqliteParameter("@DynamicEndpoint", SqliteType.Text)
                {
                    Value = (object?)outboxMessage.Endpoint.DynamicEndpoint ?? DBNull.Value
                },
                new SqliteParameter("@Created", SqliteType.Integer)
                {
                    Value = DateTime.UtcNow
                }
            ],
            _settings.DbCommandTimeout,
            context);
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.ISilverbackContext?)" />
    public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            [
                new SqliteParameter("@Content", SqliteType.Blob),
                new SqliteParameter("@Headers", SqliteType.Text),
                new SqliteParameter("@EndpointName", SqliteType.Text),
                new SqliteParameter("@DynamicEndpoint", SqliteType.Text),
                new SqliteParameter("@Created", SqliteType.Integer)
                {
                    Value = DateTime.UtcNow
                }
            ],
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Content;
                parameters[1].Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers);
                parameters[2].Value = outboxMessage.Endpoint.FriendlyName;
                parameters[3].Value = (object?)outboxMessage.Endpoint.DynamicEndpoint ?? DBNull.Value;
            },
            _settings.DbCommandTimeout,
            context);
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IAsyncEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.ISilverbackContext?)" />
    public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            [
                new SqliteParameter("@Content", SqliteType.Blob),
                new SqliteParameter("@Headers", SqliteType.Text),
                new SqliteParameter("@EndpointName", SqliteType.Text),
                new SqliteParameter("@DynamicEndpoint", SqliteType.Text),
                new SqliteParameter("@Created", SqliteType.Integer)
                {
                    Value = DateTime.UtcNow
                }
            ],
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Content;
                parameters[1].Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers);
                parameters[2].Value = outboxMessage.Endpoint.FriendlyName;
                parameters[3].Value = (object?)outboxMessage.Endpoint.DynamicEndpoint ?? DBNull.Value;
            },
            _settings.DbCommandTimeout,
            context);
    }
}
