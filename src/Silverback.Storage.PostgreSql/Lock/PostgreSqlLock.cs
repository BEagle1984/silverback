// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Postgres;

namespace Silverback.Lock;

/// <summary>
///     The distributed lock based on PostgreSql.
/// </summary>
/// TODO: TEST
public sealed class PostgreSqlLock : DistributedLock
{
    private readonly PostgreSqlLockSettings _settings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    public PostgreSqlLock(PostgreSqlLockSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        PostgresDistributedLock distributedLock = new(new PostgresAdvisoryLockKey(_settings.LockName, true), _settings.ConnectionString);

        return new PostgreSqlLockHandle(await distributedLock.AcquireAsync(cancellationToken: cancellationToken).ConfigureAwait(false));
    }

    private sealed class PostgreSqlLockHandle : DistributedLockHandle
    {
        private readonly PostgresDistributedLockHandle _innerHandle;

        private bool _isDisposed;

        public PostgreSqlLockHandle(PostgresDistributedLockHandle innerHandle)
        {
            _innerHandle = innerHandle;
        }

        public override CancellationToken LockLostToken => _innerHandle.HandleLostToken;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            _isDisposed = true;
            _innerHandle.Dispose();
        }

        protected override ValueTask DisposeCoreAsync() => _innerHandle.DisposeAsync();
    }
}
