// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Util;

namespace Silverback.Background
{
    /// <inheritdoc />
    public class DbDistributedLockManager : IDistributedLockManager
    {
        private static readonly IDistributedLockManager NullLockManager = new NullLockManager();

        private readonly ILogger<DbDistributedLockManager> _logger;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbDistributedLockManager" /> class.
        /// </summary>
        /// <param name="serviceScopeFactory">
        ///     The <see cref="IServiceScopeFactory" /> used to resolve the scoped types.
        /// </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        public DbDistributedLockManager(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DbDistributedLockManager> logger)
        {
            _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc />
        public async Task<DistributedLock?> Acquire(
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
                return await NullLockManager.Acquire(settings, cancellationToken);

            _logger.LogInformation(
                "Trying to acquire lock {lockName} ({lockUniqueId})...",
                settings.ResourceName,
                settings.UniqueId);

            var stopwatch = Stopwatch.StartNew();
            while (settings.AcquireTimeout == null || stopwatch.Elapsed < settings.AcquireTimeout)
            {
                if (await TryAcquireLock(settings))
                {
                    _logger.LogInformation(
                        "Acquired lock {lockName} ({lockUniqueId}).",
                        settings.ResourceName,
                        settings.UniqueId);
                    return new DistributedLock(settings, this);
                }

                await Task.Delay(settings.AcquireRetryInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            throw new TimeoutException($"Timeout waiting to get the required lock '{settings.ResourceName}'.");
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task<bool> CheckIsStillLocked(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.CheckIsStillLocked(settings);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await CheckIsStillLocked(
                    settings.ResourceName,
                    settings.UniqueId,
                    settings.HeartbeatTimeout,
                    scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to check lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName,
                    settings.UniqueId);

                return false;
            }
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task<bool> SendHeartbeat(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.SendHeartbeat(settings);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await SendHeartbeat(settings.ResourceName, settings.UniqueId, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to send heartbeat for lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName,
                    settings.UniqueId);

                return false;
            }
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task Release(DistributedLockSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            if (settings is NullLockSettings)
                await NullLockManager.Release(settings);

            var tryCount = 1;
            while (tryCount <= 3)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await Release(settings.ResourceName, settings.UniqueId, scope.ServiceProvider);

                    _logger.LogInformation(
                        "Released lock {lockName} ({lockUniqueId}).",
                        settings.ResourceName,
                        settings.UniqueId);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to release lock '{lockName} ({lockUniqueId})'. See inner exception for details.",
                        settings.ResourceName,
                        settings.UniqueId);

                    tryCount++;
                }
            }
        }

        private static async Task<bool> AcquireLock(DistributedLockSettings settings, IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = GetHeartbeatThreshold(settings.HeartbeatTimeout);
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            if (await dbSet.AsQueryable()
                .AnyAsync(l => l.Name == settings.ResourceName && l.Heartbeat >= heartbeatThreshold))
                return false;

            return await WriteLock(settings.ResourceName, settings.UniqueId, heartbeatThreshold, dbSet, dbContext);
        }

        private static async Task<bool> WriteLock(
            string resourceName,
            string uniqueId,
            DateTime heartbeatThreshold,
            IDbSet<Lock> dbSet,
            IDbContext dbContext)
        {
            var entity = await dbSet.AsQueryable().FirstOrDefaultAsync(e => e.Name == resourceName)
                         ?? dbSet.Add(new Lock { Name = resourceName });

            // Check once more to ensure that no lock was created in the meanwhile
            if (entity.UniqueId != uniqueId && entity.Heartbeat >= heartbeatThreshold)
                return false;

            entity.UniqueId = uniqueId;
            entity.Heartbeat = entity.Created = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return true;
        }

        private static async Task<bool> CheckIsStillLocked(
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
                     l.Heartbeat >= heartbeatThreshold);
        }

        private static async Task<bool> SendHeartbeat(
            string resourceName,
            string uniqueId,
            IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = await dbSet.AsQueryable()
                .FirstOrDefaultAsync(l => l.Name == resourceName && l.UniqueId == uniqueId);

            if (lockRecord == null)
                return false;

            lockRecord.Heartbeat = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return true;
        }

        private static (IDbSet<Lock> dbSet, IDbContext dbContext) GetDbSet(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<IDbContext>();
            var dbSet = dbContext.GetDbSet<Lock>();

            return (dbSet, dbContext);
        }

        private static DateTime GetHeartbeatThreshold(TimeSpan heartbeatTimeout) =>
            DateTime.UtcNow.Subtract(heartbeatTimeout);

        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryAcquireLock(DistributedLockSettings settings)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                return await AcquireLock(settings, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to acquire lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName,
                    settings.UniqueId);
            }

            return false;
        }

        private static async Task Release(string resourceName, string uniqueId, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = await dbSet.AsQueryable()
                .FirstOrDefaultAsync(l => l.Name == resourceName && l.UniqueId == uniqueId);

            if (lockRecord == null)
                return;

            dbSet.Remove(lockRecord);

            await dbContext.SaveChangesAsync();
        }
    }
}
