// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     The distributed lock based on a PostgreSql table.
/// </summary>
public class EntityFrameworkLock : TableBasedDistributedLock
{
    private readonly EntityFrameworkLockSettings _settings;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ISilverbackLogger<EntityFrameworkLock> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    public EntityFrameworkLock(
        EntityFrameworkLockSettings settings,
        IServiceScopeFactory serviceScopeFactory,
        ISilverbackLogger<EntityFrameworkLock> logger)
        : base(settings, logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="TableBasedDistributedLock.TryAcquireLockAsync" />
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "AddAsync on the DbSet doesn't add any value")]
    protected override async Task<bool> TryAcquireLockAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        DateTime now = DateTime.UtcNow;
        DateTime expiredHeartbeat = now - _settings.LockTimeout;

        // Atomically take over the existing lock, if it's free or expired
        int affected = await dbContext.Set<SilverbackLock>()
            .Where(
                lockEntity => lockEntity.LockName == _settings.LockName &&
                              (lockEntity.Handler == null || lockEntity.LastHeartbeat == null || lockEntity.LastHeartbeat < expiredHeartbeat))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(lockEntity => lockEntity.Handler, handlerName)
                    .SetProperty(lockEntity => lockEntity.AcquiredOn, now)
                    .SetProperty(lockEntity => lockEntity.LastHeartbeat, now))
            .ConfigureAwait(false);

        if (affected > 0)
            return true;

        bool exists = await dbContext.Set<SilverbackLock>()
            .AnyAsync(lockEntity => lockEntity.LockName == _settings.LockName)
            .ConfigureAwait(false);

        if (exists)
            return false;

        // The row is created only the first time and is then kept, so that the insert race can only occur once
        dbContext.Set<SilverbackLock>().Add(
            new SilverbackLock
            {
                LockName = _settings.LockName,
                Handler = handlerName,
                AcquiredOn = now,
                LastHeartbeat = now
            });

        try
        {
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Probably another process has created the lock in the meanwhile
            _logger.LogAcquireLockConcurrencyException(_settings.LockName, ex);
            return false;
        }
    }

    /// <inheritdoc cref="TableBasedDistributedLock.UpdateHeartbeatAsync" />
    protected override async Task UpdateHeartbeatAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        int affected = await dbContext.Set<SilverbackLock>()
            .Where(lockEntity => lockEntity.LockName == _settings.LockName && lockEntity.Handler == handlerName)
            .ExecuteUpdateAsync(setters => setters.SetProperty(lockEntity => lockEntity.LastHeartbeat, DateTime.UtcNow))
            .ConfigureAwait(false);

        if (affected == 0)
            throw new InvalidOperationException($"The lock {_settings.LockName} is not held by the current handler anymore.");
    }

    /// <inheritdoc cref="TableBasedDistributedLock.ReleaseLockAsync" />
    protected override async Task ReleaseLockAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        // Keep the row and just clear the handler, to avoid the insert race on the next acquisition
        await dbContext.Set<SilverbackLock>()
            .Where(lockEntity => lockEntity.LockName == _settings.LockName && lockEntity.Handler == handlerName)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(lockEntity => lockEntity.Handler, (string?)null)
                    .SetProperty(lockEntity => lockEntity.AcquiredOn, (DateTime?)null)
                    .SetProperty(lockEntity => lockEntity.LastHeartbeat, (DateTime?)null))
            .ConfigureAwait(false);
    }
}
