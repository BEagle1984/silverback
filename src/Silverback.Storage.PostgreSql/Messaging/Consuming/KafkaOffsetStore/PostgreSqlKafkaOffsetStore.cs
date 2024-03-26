// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Silverback.Messaging.Broker;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Stores the latest consumed offsets in PostgreSql.
/// </summary>
public class PostgreSqlKafkaOffsetStore : IKafkaOffsetStore
{
    private readonly PostgreSqlKafkaOffsetStoreSettings _settings;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly string _getQuerySql;

    private readonly string _insertOrReplaceSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlKafkaOffsetStore" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The offset store settings.
    /// </param>
    public PostgreSqlKafkaOffsetStore(PostgreSqlKafkaOffsetStoreSettings settings)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _dataAccess = new PostgreSqlDataAccess(_settings.ConnectionString);

        _getQuerySql = $"SELECT \"Topic\", \"Partition\", \"Offset\" FROM \"{settings.TableName}\" WHERE \"GroupId\" = @GroupId";

        _insertOrReplaceSql = $"INSERT INTO \"{settings.TableName}\" (\"GroupId\", \"Topic\", \"Partition\", \"Offset\") " +
                              "VALUES(@GroupId, @Topic, @Partition, @Offset) " +
                              "ON CONFLICT (\"GroupId\", \"Topic\", \"Partition\") DO UPDATE SET \"Offset\" = @Offset";
    }

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) =>
        _dataAccess.ExecuteQuery(
            reader => new KafkaOffset(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)),
            _getQuerySql,
            new NpgsqlParameter[]
            {
                new("@GroupId", NpgsqlDbType.Text)
                {
                    Value = groupId
                }
            },
            _settings.DbCommandTimeout);

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, SilverbackContext? context = null) =>
        _dataAccess.ExecuteNonQueryAsync(
            Check.NotNull(offsets, nameof(offsets)),
            _insertOrReplaceSql,
            new[]
            {
                new NpgsqlParameter("@GroupId", NpgsqlDbType.Text)
                {
                    Value = groupId
                },
                new NpgsqlParameter("@Topic", NpgsqlDbType.Text),
                new NpgsqlParameter("@Partition", NpgsqlDbType.Integer),
                new NpgsqlParameter("@Offset", NpgsqlDbType.Integer)
            },
            (offset, parameters) =>
            {
                parameters[1].Value = offset.TopicPartition.Topic;
                parameters[2].Value = offset.TopicPartition.Partition.Value;
                parameters[3].Value = offset.Offset.Value;
            },
            _settings.DbCommandTimeout,
            context);
}
