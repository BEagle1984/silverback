// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        ///     Gets a <see cref="Task" /> that completes when the sequence went through both behaviors. This is
        ///     necessary to synchronize completion when mixing Chunking with another sequence.
        /// </summary>
        Task SequencerBehaviorsTask { get; }

        /// <summary>
        ///     Gets a <see cref="Task" /> that will complete when the processing is completed (including
        ///     commit/rollback of the transaction and the offsets).
        /// </summary>
        Task ProcessingCompletedTask { get; }

        /// <summary>
        ///     Sets a value indicating whether the first message in the sequence was consumed and this instance was
        ///     just created.
        /// </summary>
        /// <param name="value">
        ///     The value to be set.
        /// </param>
        void SetIsNew(bool value);

        /// <summary>
        ///     Completes the <see cref="Task" /> that is used to notify when the sequence went through both
        ///     behaviors. See <see cref="SequencerBehaviorsTask" />.
        /// </summary>
        void CompleteSequencerBehaviorsTask();

        /// <summary>
        ///     Completes the <see cref="Task" /> that is used to notify when the processing is completed. See
        ///     <see cref="ProcessingCompletedTask" />.
        /// </summary>
        void NotifyProcessingCompleted();

        /// <summary>
        ///     Marks the <see cref="Task" /> that is used to notify when the processing is completed as faulted. See
        ///     <see cref="ProcessingCompletedTask" />.
        /// </summary>
        /// <param name="exception">
        ///     The exception to be set.
        /// </param>
        void NotifyProcessingFailed(Exception exception);
    }
}
