// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Silverback.Diagnostics;
using Silverback.Storage.DataAccess;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     The distributed lock based on a PostgreSql table.
/// </summary>
public class PostgreSqlTableLock : TableBasedDistributedLock
{
    private readonly PostgreSqlTableLockSettings _settings;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly string _acquireLockSql;

    private readonly string _heartbeatSql;

    private readonly string _releaseLockSql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlTableLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    public PostgreSqlTableLock(PostgreSqlTableLockSettings settings, ISilverbackLogger<PostgreSqlTableLock> logger)
        : base(settings, logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));

        _dataAccess = new PostgreSqlDataAccess(Check.NotNull(settings, nameof(settings)).ConnectionString);

        _acquireLockSql = $"INSERT INTO \"{settings.TableName}\" (LockName, Handler, AcquiredOn, LastHeartbeat) " +
                          "SELECT @lockName, @handler, NOW(), NOW() " +
                          "WHERE NOT EXISTS (" +
                          $"   SELECT 1 FROM \"{settings.TableName}\"" +
                          "    WHERE LockName = @lockName AND Handler IS NOT NULL AND " +
                          "          LastHeartbeat >= NOW() - @lockTimeout * INTERVAL '1 second')";

        _heartbeatSql = $"UPDATE \"{settings.TableName}\" " +
                        "SET LastHeartbeat = NOW() " +
                        "WHERE LockName = @lockName AND Handler = @handler";

        _releaseLockSql = $"DELETE FROM \"{settings.TableName}\" " +
                          "WHERE LockName = @lockName AND Handler = @handler";
    }

    /// <inheritdoc cref="TableBasedDistributedLock.TryAcquireLockAsync" />
    protected override async Task<bool> TryAcquireLockAsync(string handlerName)
    {
        int affected = await _dataAccess.ExecuteNonQueryAsync(
            _acquireLockSql,
            new NpgsqlParameter[]
            {
                new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
                new("@handler", NpgsqlDbType.Text) { Value = handlerName },
                new("@lockTimeout", NpgsqlDbType.Integer) { Value = _settings.LockTimeout.TotalSeconds }
            },
            _settings.DbCommandTimeout).ConfigureAwait(false);

        return affected == 1;
    }

    /// <inheritdoc cref="TableBasedDistributedLock.UpdateHeartbeatAsync" />
    protected override Task UpdateHeartbeatAsync(string handlerName) =>
        _dataAccess.ExecuteNonQueryAsync(
            _heartbeatSql,
            new NpgsqlParameter[]
            {
                new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
                new("@handler", NpgsqlDbType.Text) { Value = handlerName }
            },
            _settings.DbCommandTimeout);

    /// <inheritdoc cref="TableBasedDistributedLock.ReleaseLockAsync" />
    protected override Task ReleaseLockAsync(string handlerName) => _dataAccess.ExecuteNonQueryAsync(
        _releaseLockSql,
        new NpgsqlParameter[]
        {
            new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
            new("@handler", NpgsqlDbType.Text) { Value = handlerName }
        },
        _settings.DbCommandTimeout);
}
