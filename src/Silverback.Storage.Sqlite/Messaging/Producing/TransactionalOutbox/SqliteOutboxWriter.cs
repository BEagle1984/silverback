// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the Sqlite outbox.
/// </summary>
public class SqliteOutboxWriter : IOutboxWriter
{
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
        Check.NotNull(settings, nameof(settings));
        _dataAccess = new SqliteDataAccess(settings.ConnectionString);

        _insertSql = $"INSERT INTO {settings.TableName} (" +
                     "MessageType," +
                     "Content," +
                     "Headers," +
                     "EndpointRawName," +
                     "EndpointFriendlyName," +
                     "SerializedEndpoint," +
                     "Created" +
                     ") VALUES (" +
                     "@MessageType," +
                     "@Content," +
                     "@Headers," +
                     "@EndpointRawName," +
                     "@EndpointFriendlyName," +
                     "@SerializedEndpoint," +
                     "@Created)";
    }

    /// <inheritdoc cref="AddAsync" />
    public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessage, nameof(outboxMessage));

        return _dataAccess.ExecuteNonQueryAsync(
            context,
            _insertSql,
            SqliteDataAccess.CreateParameter("@MessageType", outboxMessage.MessageType?.AssemblyQualifiedName),
            SqliteDataAccess.CreateParameter("@Content", outboxMessage.Content),
            SqliteDataAccess.CreateParameter("@Headers", outboxMessage.Headers == null ? DBNull.Value : JsonSerializer.Serialize(outboxMessage.Headers)),
            SqliteDataAccess.CreateParameter("@EndpointRawName", outboxMessage.Endpoint.RawName),
            SqliteDataAccess.CreateParameter("@EndpointFriendlyName", outboxMessage.Endpoint.FriendlyName),
            SqliteDataAccess.CreateParameter("@SerializedEndpoint", outboxMessage.Endpoint.SerializedEndpoint),
            SqliteDataAccess.CreateParameter("@Created", DateTime.UtcNow));
    }
}
