// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
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
        private readonly IReadOnlyCollection<ISequenceReader> _sequenceReaders;

        public SequencerConsumerBehavior(IEnumerable<ISequenceReader> sequenceReaders)
        {
            _sequenceReaders = sequenceReaders.ToList();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            foreach (var sequenceReader in _sequenceReaders)
            {
                if (sequenceReader.CanHandleSequence(context.Envelope))
                {
                    var sequence = await sequenceReader.HandleSequence(context.Envelope).ConfigureAwait(false);

                    // If no sequence is returned it means by convention that the consumer started
                    // in the middle of a sequence, so the message is ignored.
                    if (sequence == null)
                    {
                        // TODO: LOG!!
                        return;
                    }

                    ((RawInboundEnvelope)context.Envelope).Sequence = sequence;

                    break;
                }
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
