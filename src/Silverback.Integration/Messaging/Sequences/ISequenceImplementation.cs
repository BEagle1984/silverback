// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     A set of logically related messages, like the chunks belonging to the same message or the
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
        ///     commit/rollback of the transaction).
        /// </summary>
        Task ProcessingCompletedTask { get; }

        /// <summary>
        ///     Gets a value indicating whether this sequence should create a new activity. This should be true if the
        ///     sequence contains independent messages.
        /// </summary>
        bool ShouldCreateNewActivity { get; }

        /// <summary>
        ///     Gets the Activity created for this sequence.
        /// </summary>
        Activity? Activity { get; }

        /// <summary>
        ///     Sets a value indicating whether the first message in the sequence was consumed and this instance was
        ///     just created.
        /// </summary>
        /// <param name="value">
        ///     The value to be set.
        /// </param>
        void SetIsNew(bool value);

        /// <summary>
        ///     Sets the <see cref="ISequence" /> that were added to this sequence (e.g. the
        ///     <see cref="ChunkSequence" /> whose aggregated message is added to a <see cref="BatchSequence" />.
        /// </summary>
        /// <param name="parentSequence">
        ///     The parent sequence.
        /// </param>
        void SetParentSequence(ISequence parentSequence);

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

        /// <summary>
        ///     Sets the Activity associated with this sequence.
        /// </summary>
        /// <param name="activity">
        ///     The activity.
        /// </param>
        void SetActivity(Activity activity);
    }
}
