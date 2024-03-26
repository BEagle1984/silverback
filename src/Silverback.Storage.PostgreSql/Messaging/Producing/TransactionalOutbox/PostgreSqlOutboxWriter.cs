// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the PostgreSql outbox.
/// </summary>
public class PostgreSqlOutboxWriter : IOutboxWriter
{
    private readonly PostgreSqlOutboxSettings _settings;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly string _insertSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlOutboxWriter" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public PostgreSqlOutboxWriter(PostgreSqlOutboxSettings settings)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new PostgreSqlDataAccess(settings.ConnectionString);

        _insertSql = $"INSERT INTO \"{settings.TableName}\" (" +
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

    /// <inheritdoc cref="AddAsync(Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage,Silverback.SilverbackContext?)" />
    public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessage, nameof(outboxMessage));

        return _dataAccess.ExecuteNonQueryAsync(
            _insertSql,
            new NpgsqlParameter[]
            {
                new("@Content", NpgsqlDbType.Bytea)
                {
                    Value = outboxMessage.Content
                },
                new("@Headers", NpgsqlDbType.Text)
                {
                    Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers)
                },
                new("@EndpointName", NpgsqlDbType.Text)
                {
                    Value = outboxMessage.Endpoint.FriendlyName
                },
                new("@DynamicEndpoint", NpgsqlDbType.Text)
                {
                    Value = (object?)outboxMessage.Endpoint.DynamicEndpoint ?? DBNull.Value
                },
                new("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            },
            _settings.DbCommandTimeout,
            context);
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.SilverbackContext?)" />
    public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            new[]
            {
                new NpgsqlParameter("@Content", NpgsqlDbType.Bytea),
                new NpgsqlParameter("@Headers", NpgsqlDbType.Text),
                new NpgsqlParameter("@EndpointName", NpgsqlDbType.Text),
                new NpgsqlParameter("@DynamicEndpoint", NpgsqlDbType.Text),
                new NpgsqlParameter("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            },
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

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IAsyncEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.SilverbackContext?)" />
    public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            new[]
            {
                new NpgsqlParameter("@Content", NpgsqlDbType.Bytea),
                new NpgsqlParameter("@Headers", NpgsqlDbType.Text),
                new NpgsqlParameter("@EndpointName", NpgsqlDbType.Text),
                new NpgsqlParameter("@DynamicEndpoint", NpgsqlDbType.Text),
                new NpgsqlParameter("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            },
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
