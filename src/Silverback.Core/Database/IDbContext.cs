// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Database
{
    /// <summary>
    ///     Abstracts the <c>DbContext</c> functionality to allow for multiple and decoupled implementations.
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        ///     Returns an <see cref="IDbSet{TEntity}" /> for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">
        ///     The type of the entity.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IDbSet{TEntity}" />.
        /// </returns>
        IDbSet<TEntity> GetDbSet<TEntity>()
            where TEntity : class;

        /// <summary>
        ///     Saves all changes made in this context to the database.
        /// </summary>
        void SaveChanges();

        /// <summary>
        ///     Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
