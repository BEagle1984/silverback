// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Background.Model;
using Silverback.Database;

namespace Silverback.Background
{
    public class DbDistributedLockManager : IDistributedLockManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private static readonly IDistributedLockManager NullLockManager = new NullLockManager();

        public DbDistributedLockManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<DbDistributedLockManager>>();
        }

        public async Task<DistributedLock> Acquire(
            DistributedLockSettings settings,
            CancellationToken cancellationToken = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.ResourceName)) 
                throw new InvalidOperationException(
                    "ResourceName cannot be null. Please provide a valid resource name in the settings.");
            
            if (settings is NullLockSettings)
                return await NullLockManager.Acquire(settings, cancellationToken);

            _logger.LogInformation("Trying to acquire lock {lockName} ({lockUniqueId})...", settings.ResourceName,
                settings.UniqueId);

            var stopwatch = Stopwatch.StartNew();
            while (settings.AcquireTimeout == null || stopwatch.Elapsed < settings.AcquireTimeout)
            {
                if (await TryAcquireLock(settings))
                {
                    _logger.LogInformation("Acquired lock {lockName} ({lockUniqueId}).", settings.ResourceName,
                        settings.UniqueId);
                    return new DistributedLock(settings, this);
                }

                await Task.Delay(settings.AcquireRetryInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            throw new TimeoutException($"Timeout waiting to get the required lock '{settings.ResourceName}'.");
        }

        public async Task<bool> CheckIsStillLocked(DistributedLockSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.CheckIsStillLocked(settings);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                return await CheckIsStillLocked(settings.ResourceName, settings.UniqueId, settings.HeartbeatTimeout,
                    scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to check lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName, settings.UniqueId);
            }

            return false;
        }

        public async Task<bool> SendHeartbeat(DistributedLockSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings is NullLockSettings)
                return await NullLockManager.SendHeartbeat(settings);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                return await SendHeartbeat(settings.ResourceName, settings.UniqueId, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "Failed to send heartbeat for lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName, settings.UniqueId);

                return false;
            }
        }

        public async Task Release(DistributedLockSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings is NullLockSettings)
                await NullLockManager.Release(settings);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                await Release(settings.ResourceName, settings.UniqueId, scope.ServiceProvider);

                _logger.LogInformation("Released lock {lockName} ({lockUniqueId}).", settings.ResourceName,
                    settings.UniqueId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Failed to release lock '{lockName} ({lockUniqueId})'. See inner exception for details.",
                    settings.ResourceName, settings.UniqueId);
            }
        }

        private async Task<bool> TryAcquireLock(DistributedLockSettings settings)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                return await AcquireLock(settings, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "Failed to acquire lock {lockName} ({lockUniqueId}). See inner exception for details.",
                    settings.ResourceName, settings.UniqueId);
            }

            return false;
        }

        private async Task<bool> AcquireLock(DistributedLockSettings settings, IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = GetHeartbeatThreshold(settings.HeartbeatTimeout);
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            if (await dbSet.AsQueryable()
                .AnyAsync(l => l.Name == settings.ResourceName && l.Heartbeat >= heartbeatThreshold))
                return false;

            return await WriteLock(settings.ResourceName, settings.UniqueId, heartbeatThreshold, dbSet, dbContext);
        }

        private async Task<bool> WriteLock(
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

        private async Task<bool> CheckIsStillLocked(
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

        private async Task<bool> SendHeartbeat(string resourceName, string uniqueId, IServiceProvider serviceProvider)
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

        private async Task Release(string resourceName, string uniqueId, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = await dbSet.AsQueryable()
                .FirstOrDefaultAsync(l => l.Name == resourceName && l.UniqueId == uniqueId);

            if (lockRecord == null)
                return;

            dbSet.Remove(lockRecord);

            await dbContext.SaveChangesAsync();
        }

        private (IDbSet<Lock> dbSet, IDbContext dbContext) GetDbSet(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<IDbContext>();
            var dbSet = dbContext.GetDbSet<Lock>();

            return (dbSet, dbContext);
        }

        private static DateTime GetHeartbeatThreshold(TimeSpan heartbeatTimeout) =>
            DateTime.UtcNow.Subtract(heartbeatTimeout);
    }
}