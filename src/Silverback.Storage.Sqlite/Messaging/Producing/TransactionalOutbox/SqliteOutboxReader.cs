// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Silverback.Messaging.Messages;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Reads from the Sqlite outbox.
/// </summary>
public class SqliteOutboxReader : IOutboxReader
{
    private readonly SqliteDataAccess _dataAccess;

    private readonly string _getQuerySql;

    private readonly string _countQuerySql;

    private readonly string _minCreatedQuerySql;

    private readonly string _deleteSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxReader" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public SqliteOutboxReader(SqliteOutboxSettings settings)
    {
        _dataAccess = new SqliteDataAccess(Check.NotNull(settings, nameof(settings)).ConnectionString);

        _getQuerySql = "SELECT " +
                       "Id," +
                       "MessageType," +
                       "Content," +
                       "Headers," +
                       "EndpointRawName," +
                       "EndpointFriendlyName," +
                       "SerializedEndpoint " +
                       $"FROM {settings.TableName} " +
                       "ORDER BY Created LIMIT @Limit";

        _countQuerySql = $"SELECT COUNT(*) FROM {settings.TableName}";

        _minCreatedQuerySql = $"SELECT MIN(Created) FROM {settings.TableName}";

        _deleteSql = $"DELETE FROM {settings.TableName} WHERE Id = @Id ";
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "GetFieldValueAsync is not async")]
    public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) =>
        _dataAccess.ExecuteQueryAsync(MapOutboxMessage, _getQuerySql, new SqliteParameter("@Limit", count));

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public async Task<int> GetLengthAsync() => (int)await _dataAccess.ExecuteScalarAsync<long>(_countQuerySql).ConfigureAwait(false);

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public async Task<TimeSpan> GetMaxAgeAsync()
    {
        string? oldestCreated = await _dataAccess.ExecuteScalarAsync<string>(_minCreatedQuerySql).ConfigureAwait(false);

        if (string.IsNullOrEmpty(oldestCreated))
            return TimeSpan.Zero;

        DateTime dateTime = DateTime.Parse(oldestCreated, CultureInfo.InvariantCulture);
        return DateTime.UtcNow - dateTime;
    }

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    // TODO: Optimize?
    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(outboxMessages, nameof(outboxMessages)).Cast<DbOutboxMessage>(),
            _deleteSql,
            new[]
            {
                new SqliteParameter("@Id", 0L)
            },
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Id;
            });

    private static OutboxMessage MapOutboxMessage(DbDataReader reader)
    {
        long id = reader.GetFieldValue<long>(0);
        string? messageType = reader.GetNullableFieldValue<string>(1);
        byte[]? content = reader.GetNullableFieldValue<byte[]>(2);
        string? headers = reader.GetNullableFieldValue<string>(3);
        string endpointRawName = reader.GetFieldValue<string>(4);
        string? endpointFriendlyName = reader.GetNullableFieldValue<string>(5);
        byte[]? serializedEndpoint = reader.GetNullableFieldValue<byte[]>(6);

        return new DbOutboxMessage(
            id,
            TypesCache.GetType(messageType),
            content,
            headers == null ? null : JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(headers),
            new OutboxMessageEndpoint(endpointRawName, endpointFriendlyName, serializedEndpoint));
    }
}
