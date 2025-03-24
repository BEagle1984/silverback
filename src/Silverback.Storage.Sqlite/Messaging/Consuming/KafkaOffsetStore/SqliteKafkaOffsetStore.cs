// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Silverback.Messaging.Broker;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Stores the latest consumed offsets in Sqlite.
/// </summary>
public class SqliteKafkaOffsetStore : IKafkaOffsetStore
{
    private readonly SqliteKafkaOffsetStoreSettings _settings;

    private readonly SqliteDataAccess _dataAccess;

    private readonly string _getQuerySql;

    private readonly string _insertOrReplaceQuerySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteKafkaOffsetStore" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The offset store settings.
    /// </param>
    public SqliteKafkaOffsetStore(SqliteKafkaOffsetStoreSettings settings)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new SqliteDataAccess(_settings.ConnectionString);

        _getQuerySql = $"SELECT Topic, Partition, Offset FROM {settings.TableName} WHERE GroupId = @GroupId";

        _insertOrReplaceQuerySql = $"INSERT OR REPLACE INTO {settings.TableName} (GroupId, Topic, Partition, Offset) " +
                                   "VALUES(@GroupId, @Topic, @Partition, @Offset)";
    }

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) =>
        _dataAccess.ExecuteQuery(
            reader => new KafkaOffset(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)),
            _getQuerySql,
            [
                new SqliteParameter("@GroupId", SqliteType.Text)
                {
                    Value = groupId
                }
            ],
            _settings.DbCommandTimeout);

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(offsets, nameof(offsets)),
            _insertOrReplaceQuerySql,
            [
                new SqliteParameter("@GroupId", SqliteType.Text)
                {
                    Value = groupId
                },
                new SqliteParameter("@Topic", SqliteType.Text),
                new SqliteParameter("@Partition", SqliteType.Integer),
                new SqliteParameter("@Offset", SqliteType.Integer)
            ],
            (offset, parameters) =>
            {
                parameters[1].Value = offset.TopicPartition.Topic;
                parameters[2].Value = offset.TopicPartition.Partition.Value;
                parameters[3].Value = offset.Offset.Value;
            },
            _settings.DbCommandTimeout,
            context);
}
