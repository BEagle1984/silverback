// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
public class PostgreSqlTableLock : DistributedLock
{
    private readonly PostgreSqlTableLockSettings _settings;

    private readonly ISilverbackLogger<PostgreSqlTableLock> _logger;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly string _insertLockSql;

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
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _logger = Check.NotNull(logger, nameof(logger));

        _dataAccess = new PostgreSqlDataAccess(Check.NotNull(settings, nameof(settings)).ConnectionString);

        _insertLockSql = $"INSERT INTO \"{settings.TableName}\" (LockName, Handler, AcquiredOn, LastHeartbeat) " +
                         "SELECT @lockName, @handler, NOW(), NOW() " +
                         "WHERE NOT EXISTS (" +
                         $"   SELECT 1 FROM \"{settings.TableName}\"" +
                         "    WHERE LockName = @lockName AND " +
                         "          (Handler IS NULL OR LastHeartbeat < NOW() - @lockTimeout * INTERVAL '1 second'))";
    }

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        string handlerName = Guid.NewGuid().ToString("N");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _dataAccess.ExecuteNonQueryAsync(
                    _insertLockSql,
                    new NpgsqlParameter[]
                    {
                        new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
                        new("@handler", NpgsqlDbType.Text) { Value = handlerName },
                        new("@lockTimeout", NpgsqlDbType.Integer) { Value = _settings.LockTimeout.TotalSeconds }
                    },
                    _settings.DbCommandTimeout).ConfigureAwait(false);

                return new PostgreSqlTableLockHandle(_settings, handlerName, _dataAccess, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogAcquireLockFailed(_settings.LockName, ex);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private sealed class PostgreSqlTableLockHandle : DistributedLockHandle
    {
        private readonly PostgreSqlTableLockSettings _settings;

        private readonly string _handlerName;

        private readonly PostgreSqlDataAccess _dataAccess;

        private readonly ISilverbackLogger<PostgreSqlTableLock> _logger;

        private readonly string _heartbeatSql;

        private readonly string _releaseLockSql;

        private readonly CancellationTokenSource _lockLostTokenSource = new();

        private readonly CancellationTokenSource _disposeTokenSource = new();

        private bool _isDisposed;

        public PostgreSqlTableLockHandle(PostgreSqlTableLockSettings settings, string handlerName, PostgreSqlDataAccess dataAccess, ISilverbackLogger<PostgreSqlTableLock> logger)
        {
            _settings = settings;
            _handlerName = handlerName;
            _dataAccess = dataAccess;
            _logger = logger;

            _heartbeatSql = $"UPDATE \"{settings.TableName}\" " +
                             "SET LastHeartbeat = NOW() " +
                             "WHERE LockName = @lockName AND Handler = @handler";

            _releaseLockSql = $"DELETE FROM \"{settings.TableName}\" " +
                              "WHERE LockName = @lockName AND Handler = @handler";

            Task.Run(HeartbeatAsync).FireAndForget();
        }

        public override CancellationToken LockLostToken => _lockLostTokenSource.Token;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                AsyncHelper.RunSynchronously(DisposeCoreAsync);
        }

        [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Intentional")]
        protected override async ValueTask DisposeCoreAsync()
        {
            if (_isDisposed)
                return;

            _disposeTokenSource.Cancel();

            await ReleaseLockAsync().ConfigureAwait(false);

            _isDisposed = true;

            _lockLostTokenSource.Dispose();
            _disposeTokenSource.Dispose();
        }

        private async Task HeartbeatAsync()
        {
            while (!_disposeTokenSource.Token.IsCancellationRequested)
            {
                if (!await UpdateHeartbeatAsync().ConfigureAwait(false))
                    return;

                await Task.Delay(_settings.HeartbeatInterval, _disposeTokenSource.Token).ConfigureAwait(false);
            }
        }

        private async Task<bool> UpdateHeartbeatAsync()
        {
            try
            {
                await _dataAccess.ExecuteNonQueryAsync(
                    _heartbeatSql,
                    new NpgsqlParameter[]
                    {
                        new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
                        new("@handler", NpgsqlDbType.Text) { Value = _handlerName }
                    },
                    _settings.DbCommandTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_disposeTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogLockLost(_settings.LockName, ex);
                    _lockLostTokenSource.Cancel();
                }

                return false;
            }

            return true;
        }

        private async Task ReleaseLockAsync()
        {
            try
            {
                await _dataAccess.ExecuteNonQueryAsync(
                    _releaseLockSql,
                    new NpgsqlParameter[]
                    {
                        new("@lockName", NpgsqlDbType.Text) { Value = _settings.LockName },
                        new("@handler", NpgsqlDbType.Text) { Value = _handlerName }
                    },
                    _settings.DbCommandTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogReleaseLockFailed(_settings.LockName, ex);
            }
        }
    }
}
