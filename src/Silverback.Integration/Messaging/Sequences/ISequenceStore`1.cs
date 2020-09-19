// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The temporary store for the sequences being consumed.
    /// </summary>
    /// <typeparam name="TSequence">The type of the sequences being stored.</typeparam>
    public interface ISequenceStore<TSequence>
        where TSequence : class, ISequence
    {
        /// <summary>
        ///     Gets the sequence with the specified id.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequence" /> instance.
        /// </returns>
        TSequence? Get(object sequenceId);

        /// <summary>
        ///     Adds the specified sequence to the store.
        /// </summary>
        /// <param name="sequence">
        ///     The sequence to be added.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequence" /> instance.
        /// </returns>
        TSequence Add(TSequence sequence);

        /// <summary>
        ///     Removes the sequence with the specified id.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        void Remove(object sequenceId);
    }
}
