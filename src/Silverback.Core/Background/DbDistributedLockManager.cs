// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Background
{
    /// <summary>
    ///     Implements a lock mechanism that relies on a shared database table to synchronize different
    ///     processes.
    /// </summary>
    public class DbDistributedLockManager : IDistributedLockManager
    {
        private static readonly IDistributedLockManager NullLockManager = new NullLockManager();

        private readonly ISilverbackLogger<DbDistributedLockManager> _logger;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbDistributedLockManager" /> class.
        /// </summary>
        /// <param name="serviceScopeFactory">
        ///     The <see cref="IServiceScopeFactory" /> used to resolve the scoped types.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public DbDistributedLockManager(
            IServiceScopeFactory serviceScopeFactory,
            ISilverbackLogger<DbDistributedLockManager> logger)
        {
            _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="IDistributedLockManager.AcquireAsync" />
        public async Task<DistributedLock?> AcquireAsync(
            DistributedLockSettings settings,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(settings, nameof(settings));

            if (string.IsNullOrEmpty(settings.ResourceName))
            {
                throw new InvalidOperationException(
                    "ResourceName cannot be null. Please provide a valid resource name in the settings.");
            }

            if (settings is NullLockSettings)
                return await NullLockManager.AcquireAsync(settings, cancellationToken).ConfigureAwait(false);

            _logger.LogAcquiringLock(settings);

            var stopwatch = Stopwatch.StartNew();
            while (settings.AcquireTimeout == null || stopwatch.Elapsed < settings.AcquireTimeout)
            {
                if (await TryAcquireLockAsync(settings).ConfigureAwait(false))
                {
                    _logger.LogLockAcquired(settings);
                    return new DistributedLock(settings, this);
                }

                await Task.Delay(settings.AcquireRetryInterval, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            throw new TimeoutException(
                $"Timeout waiting to get the required lock '{settings.ResourceName}'.");
        }

        /// <inheritdoc cref="IDistributedLockManager.CheckIsStillLockedAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task<bool> CheckIsStillLockedAsync(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.CheckIsStillLockedAsync(settings).ConfigureAwait(false);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await CheckIsStillLockedAsync(
                        settings.ResourceName,
                        settings.UniqueId,
                        settings.HeartbeatTimeout,
                        scope.ServiceProvider)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToCheckLock(settings, ex);
                return false;
            }
        }

        /// <inheritdoc cref="IDistributedLockManager.SendHeartbeatAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task<bool> SendHeartbeatAsync(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.SendHeartbeatAsync(settings).ConfigureAwait(false);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await SendHeartbeatAsync(
                        settings.ResourceName,
                        settings.UniqueId,
                        scope.ServiceProvider)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToSendLockHeartbeat(settings, ex);
                return false;
            }
        }

        /// <inheritdoc cref="IDistributedLockManager.ReleaseAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task ReleaseAsync(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                await NullLockManager.ReleaseAsync(settings).ConfigureAwait(false);

            var tryCount = 1;
            while (tryCount <= 3)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await ReleaseAsync(settings.ResourceName, settings.UniqueId, scope.ServiceProvider)
                        .ConfigureAwait(false);

                    _logger.LogLockReleased(settings);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogFailedToReleaseLock(settings, ex);
                    tryCount++;
                }
            }
        }

        private static async Task<bool> AcquireLockAsync(
            DistributedLockSettings settings,
            IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = GetHeartbeatThreshold(settings.HeartbeatTimeout);
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            if (await dbSet.AsQueryable()
                .AnyAsync(l => l.Name == settings.ResourceName && l.Heartbeat >= heartbeatThreshold)
                .ConfigureAwait(false))
                return false;

            return await WriteLockAsync(
                    settings.ResourceName,
                    settings.UniqueId,
                    heartbeatThreshold,
                    dbSet,
                    dbContext)
                .ConfigureAwait(false);
        }

        private static async Task<bool> WriteLockAsync(
            string resourceName,
            string uniqueId,
            DateTime heartbeatThreshold,
            IDbSet<Lock> dbSet,
            IDbContext dbContext)
        {
            var entity = await dbSet.AsQueryable().FirstOrDefaultAsync(e => e.Name == resourceName)
                .ConfigureAwait(false);

            entity ??= dbSet.Add(new Lock { Name = resourceName });

            // Check once more to ensure that no lock was created in the meanwhile
            if (entity.UniqueId != uniqueId && entity.Heartbeat >= heartbeatThreshold)
                return false;

            entity.UniqueId = uniqueId;
            entity.Heartbeat = entity.Created = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        private static async Task<bool> CheckIsStillLockedAsync(
            string resourceName,
            string uniqueId,
            TimeSpan heartbeatTimeout,
            IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = GetHeartbeatThreshold(heartbeatTimeout);
            var (dbSet, _) = GetDbSet(serviceProvider);

            return await dbSet.AsQueryable().AnyAsync(
                    l => l.Name == resourceName &&
                         l.UniqueId == uniqueId &&
                         l.Heartbeat >= heartbeatThreshold)
                .ConfigureAwait(false);
        }

        private static async Task<bool> SendHeartbeatAsync(
            string resourceName,
            string uniqueId,
            IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockEntity = await dbSet.AsQueryable()
                .FirstOrDefaultAsync(l => l.Name == resourceName && l.UniqueId == uniqueId)
                .ConfigureAwait(false);

            if (lockEntity == null)
                return false;

            lockEntity.Heartbeat = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        private static (IDbSet<Lock> DbSet, IDbContext DbContext) GetDbSet(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<IDbContext>();
            var dbSet = dbContext.GetDbSet<Lock>();

            return (dbSet, dbContext);
        }

        private static DateTime GetHeartbeatThreshold(TimeSpan heartbeatTimeout) =>
            DateTime.UtcNow.Subtract(heartbeatTimeout);

        private static async Task ReleaseAsync(
            string resourceName,
            string uniqueId,
            IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockEntity = await dbSet.AsQueryable()
                .FirstOrDefaultAsync(l => l.Name == resourceName && l.UniqueId == uniqueId)
                .ConfigureAwait(false);

            if (lockEntity == null)
                return;

            dbSet.Remove(lockEntity);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryAcquireLockAsync(DistributedLockSettings settings)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await AcquireLockAsync(settings, scope.ServiceProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToAcquireLock(settings, ex);
            }

            return false;
        }
    }
}
