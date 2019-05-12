// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Background.Model;

namespace Silverback.Background
{
    public class DbContextDistributedLockManager<TDbContext> : IDistributedLockManager
        where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public DbContextDistributedLockManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<DbContextDistributedLockManager<TDbContext>>>();
        }

        public IDisposable Acquire(string resourceName, DistributedLockSettings settings = null) =>
            Acquire(resourceName, settings?.AcquireTimeout, settings?.AcquireRetryInterval, settings?.HeartbeatTimeout);

        public IDisposable Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null)
        {
            var start = DateTime.Now;
            while (acquireTimeout == null || DateTime.Now - start < acquireTimeout)
            {
                if (TryAcquireLock(resourceName, heartbeatTimeout))
                    return new DistributedLock(resourceName, this);

                Thread.Sleep(acquireRetryInterval?.Milliseconds ?? 500);
            }

            throw new TimeoutException($"Timeout waiting to get the required lock '{resourceName}'.");
        }

        public void SendHeartbeat(string resourceName)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    SendHeartbeat(resourceName, scope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send heartbeat for lock '{lockName}'. See inner exception for details.",
                    resourceName);
            }
        }

        public void Release(string resourceName)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    Release(resourceName, scope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock '{lockName}'. See inner exception for details.", resourceName);
            }
        }

        private bool TryAcquireLock(string resourceName, TimeSpan? heartbeatTimeout = null)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    return AcquireLock(resourceName, heartbeatTimeout, scope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire lock '{lockName}'. See inner exception for details.",
                    resourceName);
            }

            return false;
        }

        private bool AcquireLock(string resourceName, TimeSpan? heartbeatTimeout, IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = DateTime.UtcNow.Subtract(heartbeatTimeout ?? TimeSpan.FromSeconds(10));

            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            if (dbSet.Any(l => l.Name == resourceName && l.Heartbeat >= heartbeatThreshold))
                return false;

            WriteLock(resourceName, dbSet, dbContext);

            return true;
        }

        private void WriteLock(string resourceName, DbSet<Lock> dbSet, TDbContext dbContext)
        {
            var entity = dbSet.FirstOrDefault(e => e.Name == resourceName)
                         ?? dbSet.Add(new Lock { Name = resourceName }).Entity;

            entity.Heartbeat = entity.Created = DateTime.UtcNow;

            dbContext.SaveChanges();
        }
        
        private void SendHeartbeat(string resourceName, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = dbSet.FirstOrDefault(l => l.Name == resourceName);

            if (lockRecord == null)
                return;

            lockRecord.Heartbeat = DateTime.UtcNow;

            dbContext.SaveChanges();
        }
        
        private void Release(string resourceName, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = dbSet.FirstOrDefault(l => l.Name == resourceName);

            if (lockRecord == null)
                return;

            dbSet.Remove(lockRecord);
            dbContext.SaveChanges();
        }

        private (DbSet<Lock> dbSet, TDbContext dbContext) GetDbSet(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<TDbContext>();
            var dbSet = serviceProvider.GetRequiredService<TDbContext>().Set<Lock>()
                   ?? throw new SilverbackException($"The DbContext doesn't contain a DbSet<{typeof(Lock).FullName}>.");

            return (dbSet, dbContext);
        }
    }
}
