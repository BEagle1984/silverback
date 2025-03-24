// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Silverback.Messaging.Messages;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Reads from the PostgreSql outbox.
/// </summary>
public class PostgreSqlOutboxReader : IOutboxReader
{
    private readonly PostgreSqlOutboxSettings _settings;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly string _getQuerySql;

    private readonly string _countQuerySql;

    private readonly string _minCreatedQuerySql;

    private readonly string _deleteSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlOutboxReader" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public PostgreSqlOutboxReader(PostgreSqlOutboxSettings settings)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new PostgreSqlDataAccess(_settings.ConnectionString);

        _getQuerySql = "SELECT " +
                       "Id," +
                       "Content," +
                       "Headers," +
                       "EndpointName " +
                       $"FROM \"{settings.TableName}\" " +
                       "ORDER BY Created LIMIT @Limit";

        _countQuerySql = $"SELECT COUNT(*) FROM \"{settings.TableName}\"";

        _minCreatedQuerySql = $"SELECT MIN(Created) FROM \"{settings.TableName}\"";

        _deleteSql = $"DELETE FROM \"{settings.TableName}\" WHERE Id = @Id ";
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count) =>
        _dataAccess.ExecuteQueryAsync(
            MapOutboxMessage,
            _getQuerySql,
            [
                new NpgsqlParameter("@Limit", NpgsqlDbType.Integer) { Value = count }
            ],
            _settings.DbCommandTimeout);

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public async Task<int> GetLengthAsync() =>
        (int)await _dataAccess.ExecuteScalarAsync<long>(_countQuerySql, null, _settings.DbCommandTimeout).ConfigureAwait(false);

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public async Task<TimeSpan> GetMaxAgeAsync()
    {
        DateTime oldestCreated = await _dataAccess.ExecuteScalarAsync<DateTime>(
            _minCreatedQuerySql,
            null,
            _settings.DbCommandTimeout).ConfigureAwait(false);

        if (oldestCreated == default)
            return TimeSpan.Zero;

        return DateTime.UtcNow - oldestCreated;
    }

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(outboxMessages, nameof(outboxMessages)).Cast<DbOutboxMessage>(),
            _deleteSql,
            [
                new NpgsqlParameter("@Id", NpgsqlDbType.Bigint)
            ],
            (outboxMessage, parameters) =>
            {
                parameters[0].Value = outboxMessage.Id;
            },
            _settings.DbCommandTimeout);

    private static OutboxMessage MapOutboxMessage(DbDataReader reader)
    {
        long id = reader.GetFieldValue<long>(0);
        byte[]? content = reader.GetNullableFieldValue<byte[]>(1);
        string? headers = reader.GetNullableFieldValue<string>(2);
        string endpointName = reader.GetFieldValue<string>(3);

        return new DbOutboxMessage(
            id,
            content,
            headers == null ? null : JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(headers),
            endpointName);
    }
}
