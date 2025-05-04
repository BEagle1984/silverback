// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Domain;

/// <summary>
///     Can be used to automatically publish the domain events stored into the domain entities being saved via Entity Framework.
/// </summary>
/// <typeparam name="TDbContext">
///     The type of the <see cref="DbContext" /> to be managed.
/// </typeparam>
public class EntityFrameworkDomainEventsPublisher<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    private readonly Func<bool, int> _saveChanges;

    private readonly Func<bool, CancellationToken, Task<int>> _saveChangesAsync;

    private readonly IPublisher _publisher;

    private readonly DomainEventsPublisher _domainEventsPublisher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkDomainEventsPublisher{TDbContext}" /> class.
    /// </summary>
    /// <param name="dbContext">
    ///     The <see cref="DbContext" /> to be managed.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> to be used to publish the events.
    /// </param>
    public EntityFrameworkDomainEventsPublisher(TDbContext dbContext, IPublisher publisher)
        : this(
            dbContext,
            Check.NotNull(dbContext, nameof(dbContext)).SaveChanges,
            Check.NotNull(dbContext, nameof(dbContext)).SaveChangesAsync,
            publisher)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkDomainEventsPublisher{TDbContext}" /> class.
    /// </summary>
    /// <param name="dbContext">
    ///     The <see cref="DbContext" /> to be managed.
    /// </param>
    /// <param name="saveChanges">
    ///     The method to be used to save the changes.
    /// </param>
    /// <param name="saveChangesAsync">
    ///     The asynchronous method to be used to save the changes.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> to be used to publish the events.
    /// </param>
    public EntityFrameworkDomainEventsPublisher(
        TDbContext dbContext,
        Func<bool, int> saveChanges,
        Func<bool, CancellationToken, Task<int>> saveChangesAsync,
        IPublisher publisher)
    {
        _dbContext = Check.NotNull(dbContext, nameof(dbContext));
        _saveChanges = Check.NotNull(saveChanges, nameof(saveChanges));
        _saveChangesAsync = Check.NotNull(saveChangesAsync, nameof(saveChangesAsync));
        _publisher = Check.NotNull(publisher, nameof(publisher));

        _domainEventsPublisher = new DomainEventsPublisher(
            () => _dbContext.ChangeTracker.Entries().Select(entry => entry.Entity),
            publisher);
    }

    /// <summary>
    ///     Saves all changes made in this context to the database and publishes the domain events stored into the domain entities.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges" />
    ///     is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>
    ///     The number of state entries written to the database.
    /// </returns>
    public int SaveChangesAndPublishDomainEvents(bool acceptAllChangesOnSuccess = true)
    {
        DbTransaction? contextTransaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        IStorageTransaction? existingStorageTransaction = _publisher.Context.GetStorageTransaction();
        IStorageTransaction? newStorageTransaction = null;
        IStorageTransaction? previousStorageTransaction = null;

        if (contextTransaction == null && existingStorageTransaction != null)
        {
            // If the DbContext transaction is not set, we ensure that the storage won't use a transaction either
            previousStorageTransaction = existingStorageTransaction;
            _publisher.Context.ClearStorageTransaction();
        }
        else if (contextTransaction != null && existingStorageTransaction == null)
        {
            // If the DbContext transaction is set, we ensure that the storage will use it
            newStorageTransaction = _publisher.EnlistDbTransaction(contextTransaction);
        }
        else if (contextTransaction != null && existingStorageTransaction != null && contextTransaction != existingStorageTransaction.UnderlyingTransaction)
        {
            // If both transactions are set, but they are different, we ensure that the storage will use the DbContext transaction
            previousStorageTransaction = existingStorageTransaction;
            _publisher.Context.ClearStorageTransaction();
            newStorageTransaction = _publisher.EnlistDbTransaction(contextTransaction);
        }

        try
        {
            _domainEventsPublisher.PublishDomainEvents();
            int result = _saveChanges.Invoke(acceptAllChangesOnSuccess);
            return result;
        }
        finally
        {
            if (newStorageTransaction != null)
                _publisher.Context.ClearStorageTransaction();

            if (previousStorageTransaction != null)
                _publisher.Context.EnlistTransaction(previousStorageTransaction);
        }
    }

    /// <summary>
    ///     Saves all changes made in this context to the database and publishes the domain events stored into the domain entities.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges" />
    ///     is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancellationToken">
    ///     The token that can be used to request cancellation of the asynchronous operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the number of state entries written to
    ///     the database.
    /// </returns>
    public async Task<int> SaveChangesAndPublishDomainEventsAsync(
        bool acceptAllChangesOnSuccess = true,
        CancellationToken cancellationToken = default)
    {
        DbTransaction? contextTransaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        IStorageTransaction? existingStorageTransaction = _publisher.Context.GetStorageTransaction();
        IStorageTransaction? newStorageTransaction = null;
        IStorageTransaction? previousStorageTransaction = null;

        if (contextTransaction == null && existingStorageTransaction != null)
        {
            // If the DbContext transaction is not set, we ensure that the storage won't use a transaction either
            previousStorageTransaction = existingStorageTransaction;
            _publisher.Context.ClearStorageTransaction();
        }
        else if (contextTransaction != null && existingStorageTransaction == null)
        {
            // If the DbContext transaction is set, we ensure that the storage will use it
            newStorageTransaction = _publisher.EnlistDbTransaction(contextTransaction);
        }
        else if (contextTransaction != null && existingStorageTransaction != null && contextTransaction != existingStorageTransaction.UnderlyingTransaction)
        {
            // If both transactions are set, but they are different, we ensure that the storage will use the DbContext transaction
            previousStorageTransaction = existingStorageTransaction;
            _publisher.Context.ClearStorageTransaction();
            newStorageTransaction = _publisher.EnlistDbTransaction(contextTransaction);
        }

        try
        {
            await _domainEventsPublisher.PublishDomainEventsAsync().ConfigureAwait(false);
            int result = await _saveChangesAsync.Invoke(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally
        {
            if (newStorageTransaction != null)
                _publisher.Context.ClearStorageTransaction();

            if (previousStorageTransaction != null)
                _publisher.Context.EnlistTransaction(previousStorageTransaction);
        }
    }
}
