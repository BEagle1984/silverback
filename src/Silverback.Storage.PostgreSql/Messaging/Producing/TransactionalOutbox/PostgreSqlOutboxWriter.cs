// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
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
                     "Created" +
                     ") VALUES (" +
                     "@Content," +
                     "@Headers," +
                     "@EndpointName," +
                     "@Created)";
    }

    /// <inheritdoc cref="AddAsync(OutboxMessage,ISilverbackContext?,CancellationToken)" />
    public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessage, nameof(outboxMessage));

        return _dataAccess.ExecuteNonQueryAsync(
            _insertSql,
            [
                new NpgsqlParameter("@Content", NpgsqlDbType.Bytea)
                {
                    Value = outboxMessage.Content
                },
                new NpgsqlParameter("@Headers", NpgsqlDbType.Text)
                {
                    Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers)
                },
                new NpgsqlParameter("@EndpointName", NpgsqlDbType.Text)
                {
                    Value = outboxMessage.EndpointName
                },
                new NpgsqlParameter("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            ],
            _settings.DbCommandTimeout,
            context,
            cancellationToken);
    }

    /// <inheritdoc cref="AddAsync(IEnumerable{OutboxMessage},ISilverbackContext?,CancellationToken)" />
    public Task AddAsync(
        IEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            [
                new NpgsqlParameter("@Content", NpgsqlDbType.Bytea),
                new NpgsqlParameter("@Headers", NpgsqlDbType.Text),
                new NpgsqlParameter("@EndpointName", NpgsqlDbType.Text),
                new NpgsqlParameter("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            ],
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Content;
                parameters[1].Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers);
                parameters[2].Value = outboxMessage.EndpointName;
            },
            _settings.DbCommandTimeout,
            context,
            cancellationToken);
    }

    /// <inheritdoc cref="AddAsync(IAsyncEnumerable{OutboxMessage},ISilverbackContext?,CancellationToken)" />
    public Task AddAsync(
        IAsyncEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        return _dataAccess.ExecuteNonQueryAsync(
            outboxMessages,
            _insertSql,
            [
                new NpgsqlParameter("@Content", NpgsqlDbType.Bytea),
                new NpgsqlParameter("@Headers", NpgsqlDbType.Text),
                new NpgsqlParameter("@EndpointName", NpgsqlDbType.Text),
                new NpgsqlParameter("@Created", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.UtcNow
                }
            ],
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Content;
                parameters[1].Value = outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers);
                parameters[2].Value = outboxMessage.EndpointName;
            },
            _settings.DbCommandTimeout,
            context,
            cancellationToken);
    }
}
