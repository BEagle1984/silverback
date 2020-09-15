// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceStore<TSequence>
        where TSequence : class, ISequence
    {
        /// <summary>
        ///     Gets the sequence with the specified id. If not found, a new one will be created on the fly using the
        ///     specified factory.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <param name="factory">
        ///     The factory that will be used to create the new sequence.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequence" /> instance.
        /// </returns>
        //TSequence GetOrAdd(object sequenceId, Func<ISequence> factory);

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
