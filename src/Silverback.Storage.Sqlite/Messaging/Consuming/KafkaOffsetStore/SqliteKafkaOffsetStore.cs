﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Silverback.Messaging.Broker;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Stores the latest consumed offsets in memory.
/// </summary>
public class SqliteKafkaOffsetStore : IKafkaOffsetStore
{
    private readonly SqliteDataAccess _dataAccess;

    private readonly string _getQuerySql;

    private readonly string _tableExistsSql;

    private readonly string _insertOrReplaceQuerySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStore" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The offset store settings.
    /// </param>
    public SqliteKafkaOffsetStore(SqliteKafkaOffsetStoreSettings settings)
    {
        _dataAccess = new SqliteDataAccess(Check.NotNull(settings, nameof(settings)).ConnectionString);

        _getQuerySql = $"SELECT Topic, Partition, Offset FROM {settings.TableName} WHERE GroupId = @GroupId";

        _tableExistsSql = $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name = '{settings.TableName}';";

        _insertOrReplaceQuerySql = $"INSERT OR REPLACE INTO {settings.TableName} " +
                                   "(GroupId, Topic, Partition, Offset) " +
                                   "VALUES(@GroupId, @Topic, @Partition, @Offset)";
    }

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId)
    {
        try
        {
            return _dataAccess.ExecuteQuery(
                reader => new KafkaOffset(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2)),
                _getQuerySql,
                new SqliteParameter("@GroupId", groupId));
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            if (_dataAccess.ExecuteScalar<long>(_tableExistsSql) == 0)
                return Array.Empty<KafkaOffset>();

            throw;
        }
    }

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, SilverbackContext? context = null) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(offsets, nameof(offsets)),
            _insertOrReplaceQuerySql,
            new[]
            {
                new SqliteParameter("@GroupId", groupId),
                new SqliteParameter("@Topic", string.Empty),
                new SqliteParameter("@Partition", 0),
                new SqliteParameter("@Offset", 0L)
            },
            (offset, parameters) =>
            {
                parameters[1].Value = offset.TopicPartition.Topic;
                parameters[2].Value = offset.TopicPartition.Partition.Value;
                parameters[3].Value = offset.Offset.Value;
            },
            context);
}