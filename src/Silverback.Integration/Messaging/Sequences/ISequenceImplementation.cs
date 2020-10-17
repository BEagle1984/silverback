// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Represents a set of logically related messages, like the chunks belonging to the same message or the
    ///     messages in a dataset.
    /// </summary>
    internal interface ISequenceImplementation : ISequence
    {
        /// <summary>
        ///     Gets the <see cref="TaskCompletionSource{TResult}" /> that is used to notify when the sequence went
        ///     through both behaviors. Necessary to synchronize completion when mixing Chunking with another
        ///     sequence.
        /// </summary>
        TaskCompletionSource<bool> SequencerBehaviorsTaskCompletionSource { get; }

        /// <summary>
        ///     Gets the <see cref="TaskCompletionSource{TResult}" /> that is used to notify when the processing is
        ///     finished (including commit/rollback of the transaction and the offsets).
        /// </summary>
        TaskCompletionSource<bool> ProcessedTaskCompletionSource { get; }

        /// <summary>
        ///     Sets a value indicating whether the first message in the sequence was consumed and this instance was
        ///     just created.
        /// </summary>
        /// <param name="value">
        ///     The value to be set.
        /// </param>
        void SetIsNew(bool value);
    }
}
