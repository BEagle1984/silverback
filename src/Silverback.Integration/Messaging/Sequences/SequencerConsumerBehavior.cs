// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Uses the available implementations of <see cref="ISequenceReader"/> to assign the incoming message to the right sequence.
    /// </summary>
    /// <remarks>
    ///    A sequence is a set of messages that are handled as a single unit of work. A sequence could be used to group all chunks belonging to the same source message, all messages belonging to the same data set or to implement batch processing.
    /// </remarks>
    public class SequencerConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO:
            // * Call each ISequencerReader
            // * Skip until we are at the beginning of a sequence

            await next(context).ConfigureAwait(false);
        }
    }

    public interface ISequencerReader
    {
        IRawInboundEnvelope? SetSequence(IRawInboundEnvelope rawInboundEnvelope);

        // TODO:
        // * Create ChunksSequenceReader
        //     * Read headers and set SequenceInfo accordingly
        //     * Ensure proper order is respected
        //     * Determine if file: either from headers or hardcoded BinaryFileSerializer
        //     * If not a file, return null until the whole message is received and aggregated
    }

    public interface ISequencerWriter
    {
        IReadOnlyCollection<IRawOutboundEnvelope> SetSequenc(IRawInboundEnvelope rawInboundEnvelope);
    }

    public class SequenceInfo : ISequenceInfo
    {
        public object SequenceId { get; }

        public int? TotalCount { get; }

        public int? Index { get; }
    }

    public interface ISequenceInfo
    {
        object SequenceId { get; }

        int? TotalCount { get; }

        int? Index { get; }
    }
}
