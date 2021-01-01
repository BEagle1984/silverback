// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The base class for the <see cref="ISequenceReader" /> implementations. It encapsulates the logic to
    ///     deal with the <see cref="ISequenceStore" />.
    /// </summary>
    public abstract class SequenceReaderBase : ISequenceReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SequenceReaderBase" /> class.
        /// </summary>
        /// <param name="handlesRawMessages">
        ///     A value indicating whether this reader handles the raw messages, before they are being deserialized,
        ///     decrypted, etc.
        /// </param>
        protected SequenceReaderBase(bool handlesRawMessages = false)
        {
            HandlesRawMessages = handlesRawMessages;
        }

        /// <inheritdoc cref="ISequenceReader.HandlesRawMessages" />
        public bool HandlesRawMessages { get; }

        /// <inheritdoc cref="ISequenceReader.CanHandleAsync" />
        public abstract Task<bool> CanHandleAsync(ConsumerPipelineContext context);

        /// <inheritdoc cref="ISequenceReader.GetSequenceAsync" />
        [SuppressMessage("", "CA2000", Justification = "Sequence is being returned")]
        public async Task<ISequence> GetSequenceAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            string sequenceId = await GetSequenceIdAsync(context).ConfigureAwait(false);
            bool isNewSequence = await IsNewSequenceAsync(sequenceId, context).ConfigureAwait(false);

            if (string.IsNullOrEmpty(sequenceId))
                throw new InvalidOperationException("Sequence identifier not found or invalid.");

            return isNewSequence
                ? await CreateNewSequenceAsync(sequenceId, context).ConfigureAwait(false)
                : await GetExistingSequenceAsync(context, sequenceId).ConfigureAwait(false) ??
                  new IncompleteSequence(sequenceId, context);
        }

        /// <summary>
        ///     Gets the sequence identifier extracted from the current envelope.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains
        ///     the recognized sequence identifier, or <c>null</c>.
        /// </returns>
        protected virtual Task<string> GetSequenceIdAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            string messageId = context.Envelope.Headers.GetValue(DefaultMessageHeaders.MessageId) ??
                               "***default***";

            return Task.FromResult(messageId);
        }

        /// <summary>
        ///     Determines if the current message correspond with the beginning of a new sequence.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains
        ///     <c>true</c> if a new sequence is starting; otherwise <c>false</c>.
        /// </returns>
        protected abstract Task<bool> IsNewSequenceAsync(string sequenceId, ConsumerPipelineContext context);

        /// <summary>
        ///     Creates the new sequence and adds it to the store.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     The new sequence.
        /// </returns>
        protected virtual async Task<ISequence> CreateNewSequenceAsync(
            string sequenceId,
            ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            await AwaitOrAbortPreviousSequencesAsync(context.SequenceStore).ConfigureAwait(false);

            return await context.SequenceStore
                .AddAsync(CreateNewSequenceCore(sequenceId, context))
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates the new sequence object.
        /// </summary>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     The new sequence.
        /// </returns>
        protected abstract ISequence CreateNewSequenceCore(
            string sequenceId,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Retrieves the existing incomplete sequence from the store.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <param name="sequenceId">
        ///     The sequence identifier.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequence" /> or <c>null</c> if not found.
        /// </returns>
        protected virtual Task<ISequence?> GetExistingSequenceAsync(
            ConsumerPipelineContext context,
            string sequenceId)
        {
            Check.NotNull(context, nameof(context));

            return context.SequenceStore.GetAsync<ISequence>(sequenceId);
        }

        private async Task AwaitOrAbortPreviousSequencesAsync(
            ISequenceStore sequenceStore)
        {
            var sequences = sequenceStore.ToList();

            await sequences
                .ForEachAsync(
                    async previousSequence =>
                    {
                        // Prevent Sequence and RawSequence to mess with each other
                        if (HandlesRawMessages && previousSequence is Sequence ||
                            !HandlesRawMessages && previousSequence is RawSequence)
                            return;

                        if (!previousSequence.IsComplete)
                        {
                            await previousSequence.AbortAsync(SequenceAbortReason.IncompleteSequence)
                                .ConfigureAwait(false);
                        }

                        var parentSequence = previousSequence.ParentSequence;

                        if (parentSequence == null && previousSequence.IsComplete)
                            await previousSequence.AwaitProcessingAsync(false).ConfigureAwait(false);

                        if (parentSequence != null && parentSequence.IsComplete)
                            await parentSequence.AwaitProcessingAsync(false).ConfigureAwait(false);
                    })
                .ConfigureAwait(false);

            await sequences
                .Where(sequence => !sequence.IsPending)
                .ForEachAsync(sequence => sequenceStore.RemoveAsync(sequence.SequenceId))
                .ConfigureAwait(false);
        }
    }
}
