// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data.Common;
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
///     Reads from the SQLite outbox.
/// </summary>
public class SqliteOutboxReader : IOutboxReader
{
    private readonly SqliteOutboxSettings _settings;

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
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new SqliteDataAccess(_settings.ConnectionString);

        _getQuerySql = "SELECT " +
                       "Id," +
                       "Content," +
                       "Headers," +
                       "EndpointName " +
                       $"FROM {settings.TableName} " +
                       "ORDER BY Created LIMIT @Limit";

        _countQuerySql = $"SELECT COUNT(*) FROM {settings.TableName}";

        _minCreatedQuerySql = $"SELECT MIN(Created) FROM {settings.TableName}";

        _deleteSql = $"DELETE FROM {settings.TableName} WHERE Id = @Id ";
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) =>
        _dataAccess.ExecuteQueryAsync(
            MapOutboxMessage,
            _getQuerySql,
            [
                new SqliteParameter("@Limit", SqliteType.Integer)
                {
                    Value = count
                }
            ],
            _settings.DbCommandTimeout);

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public async Task<int> GetLengthAsync() =>
        (int)await _dataAccess.ExecuteScalarAsync<long>(_countQuerySql, null, _settings.DbCommandTimeout).ConfigureAwait(false);

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public async Task<TimeSpan> GetMaxAgeAsync()
    {
        string? oldestCreated = await _dataAccess.ExecuteScalarAsync<string>(
            _minCreatedQuerySql,
            null,
            _settings.DbCommandTimeout).ConfigureAwait(false);

        if (string.IsNullOrEmpty(oldestCreated))
            return TimeSpan.Zero;

        DateTime dateTime = DateTime.Parse(oldestCreated, CultureInfo.InvariantCulture);
        return DateTime.UtcNow - dateTime;
    }

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(outboxMessages, nameof(outboxMessages)).Cast<DbOutboxMessage>(),
            _deleteSql,
            [
                new SqliteParameter("@Id", SqliteType.Integer)
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
