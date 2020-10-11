// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Batching
{
    public class BatchSequenceReader : SequenceReaderBase, ISorted
    {
        private BatchSequence? _currentSequence;

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => int.MaxValue; // Ignored if a proper sequence is detected

        /// <inheritdoc cref="SequenceReaderBase.CanHandleAsync" />
        public override Task<bool> CanHandleAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            bool isBatchEnabled = context.Envelope.Endpoint.Batch != null && context.Envelope.Endpoint.Batch.Size > 1;

            return Task.FromResult(isBatchEnabled);
        }

        /// <inheritdoc cref="SequenceReaderBase.GetSequenceId" />
        protected override string GetSequenceId(ConsumerPipelineContext context) => "batch";

        /// <inheritdoc cref="SequenceReaderBase.IsNewSequence" />
        protected override bool IsNewSequence(ConsumerPipelineContext context) =>
            _currentSequence == null || !_currentSequence.IsPending;

        /// <inheritdoc cref="SequenceReaderBase.CreateNewSequenceCore" />
        protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context) =>
            _currentSequence = new BatchSequence(sequenceId, context);
    }
}
