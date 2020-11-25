// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The temporary store for the sequences being consumed.
    /// </summary>
    public interface ISequenceStore : IReadOnlyCollection<ISequence>, IDisposable
    {
        /// <summary>
        ///     Gets a value indicating whether there currently are pending sequences in the store.
        /// </summary>
        bool HasPendingSequences { get; }

        /// <summary>
        ///     Gets the sequence with the specified id.
        /// </summary>
        /// <typeparam name="TSequence">
        ///     The type of the sequence to be retrieved.
        /// </typeparam>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <param name="matchPrefix">
        ///     Enables sequence id prefix matching (String.StartsWith).
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="ISequence" /> instance.
        /// </returns>
        Task<TSequence?> GetAsync<TSequence>(string sequenceId, bool matchPrefix = false)
            where TSequence : class, ISequence;

        /// <summary>
        ///     Adds the specified sequence to the store.
        /// </summary>
        /// <typeparam name="TSequence">
        ///     The type of the sequence to be added.
        /// </typeparam>
        /// <param name="sequence">
        ///     The sequence to be added.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="ISequence" /> instance.
        /// </returns>
        Task<TSequence> AddAsync<TSequence>(TSequence sequence)
            where TSequence : class, ISequence;

        /// <summary>
        ///     Removes the sequence with the specified id.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RemoveAsync(string sequenceId);
    }
}
