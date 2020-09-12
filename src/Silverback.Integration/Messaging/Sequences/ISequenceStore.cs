// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceStore
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
        /// <returns>The <see cref="ISequence" /> instance.</returns>
        ISequence GetOrAdd(object sequenceId, Action<ISequence> factory);
    }
}
