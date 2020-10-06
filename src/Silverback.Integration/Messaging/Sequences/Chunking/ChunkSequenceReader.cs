// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     Handles the chunked messages.
    /// </summary>
    public class ChunkSequenceReader : ISequenceReader
    {
        /// <inheritdoc cref="ISequenceReader.CanHandleAsync"/>
        public Task<bool> CanHandleAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            var canHandle = context.Envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex);

            return Task.FromResult(canHandle);
        }

        /// <inheritdoc cref="ISequenceReader.GetSequenceAsync"/>
        [SuppressMessage("", "CA2000", Justification = "The sequence is returned")]
        public async Task<ISequence?> GetSequenceAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            var envelope = context.Envelope;

            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                             throw new InvalidOperationException("Chunk index header not found.");

            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            return chunkIndex == 0
                ? await CreateNewSequence(context, messageId).ConfigureAwait(false)
                : await GetExistingSequence(context, messageId).ConfigureAwait(false);
        }

        private static async Task<ChunkSequence> CreateNewSequence(ConsumerPipelineContext context, string messageId)
        {
            if (context.SequenceStore.HasPendingSequences && context.Envelope.Endpoint.Sequence.ConsecutiveMessages)
                await AbortPreviousSequences(context).ConfigureAwait(false);

            var chunksCount = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            var sequence = new ChunkSequence(messageId, chunksCount.Value, context);
            await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);

            // Replace the envelope with the stream that will be pushed with all the chunks.
            context.Envelope = context.Envelope.CloneReplacingStream(new ChunkStream(sequence.Stream));
            return sequence;
        }

        private static async Task AbortPreviousSequences(ConsumerPipelineContext context)
        {
            // TODO: Log

            await context.SequenceStore.ForEachAsync(previousSequence => previousSequence.AbortAsync(false))
                .ConfigureAwait(false);
        }

        private static async Task<ChunkSequence> GetExistingSequence(ConsumerPipelineContext context, string messageId)
        {
            var sequence = await context.SequenceStore.GetAsync<ChunkSequence>(messageId).ConfigureAwait(false);

            // Skip the message if a sequence cannot be found. It means that the consumer started in the
            // middle of a sequence.
            if (sequence == null)
            {
                // TODO: Log
            }

            return sequence;
        }
    }
}
