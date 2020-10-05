// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The temporary store for the sequences being consumed.
    /// </summary>
    public interface ISequenceStore
    {
        /// <summary>
        ///     Removes the sequence with the specified id.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        void Remove(object sequenceId);
    }
}
