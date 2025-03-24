// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    protected override async Task<bool> TryAcquireLockAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        SilverbackLock? lockEntity = await dbContext.Set<SilverbackLock>()
            .AsTracking()
            .Where(lockEntity => lockEntity.LockName == _settings.LockName)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (lockEntity is { Handler: not null } && lockEntity.LastHeartbeat >= DateTime.UtcNow - _settings.LockTimeout)
            return false;

        if (lockEntity == null)
        {
            lockEntity = new SilverbackLock
            {
                LockName = _settings.LockName
            };

            dbContext.Set<SilverbackLock>().Add(lockEntity);
        }

        lockEntity.Handler = handlerName;
        lockEntity.AcquiredOn = DateTime.UtcNow;
        lockEntity.LastHeartbeat = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Probably another process has acquired the lock in the meanwhile
            _logger.LogAcquireLockConcurrencyException(_settings.LockName, ex);
            return false;
        }
    }

    /// <inheritdoc cref="TableBasedDistributedLock.UpdateHeartbeatAsync" />
    protected override async Task UpdateHeartbeatAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        SilverbackLock lockEntity = await dbContext.Set<SilverbackLock>()
            .AsTracking()
            .Where(lockEntity => lockEntity.LockName == _settings.LockName && lockEntity.Handler == handlerName)
            .FirstAsync()
            .ConfigureAwait(false);

        lockEntity.LastHeartbeat = DateTime.UtcNow;

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="TableBasedDistributedLock.ReleaseLockAsync" />
    protected override async Task ReleaseLockAsync(string handlerName)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        SilverbackLock? lockEntity = await dbContext.Set<SilverbackLock>()
            .AsTracking()
            .Where(lockEntity => lockEntity.LockName == _settings.LockName && lockEntity.Handler == handlerName)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (lockEntity != null)
        {
            dbContext.Set<SilverbackLock>().Remove(lockEntity);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
