// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Uses the available implementations of <see cref="ISequenceReader" /> to assign the incoming message to
    ///     the right sequence.
    /// </summary>
    /// <remarks>
    ///     A sequence is a set of messages that are handled as a single unit of work. A sequence could be used to
    ///     group all chunks belonging to the same source message, all messages belonging to the same data set or
    ///     to implement batch processing.
    /// </remarks>
    public class SequencerConsumerBehavior : IConsumerBehavior
    {
        private readonly IReadOnlyCollection<ISequenceReader> _sequenceReaders;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
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
                if (await TryHandleSequence(context, sequenceReader).ConfigureAwait(false))
                {
                    // If no sequence is returned it means either that the message was pushed into an existing
                    // sequence or the consumer didn't start at the beginning of a sequence. In both cases
                    // we can just break the pipeline and return.
                    if (context.Envelope.Sequence == null)
                        return;

                    break;
                }
            }

            if (context.Envelope.Sequence != null)
            {
                StartProcessingThread(context, next);
            }
            else
            {
                await next(context).ConfigureAwait(false);
            }
        }

        private static async Task<bool> TryHandleSequence(
            ConsumerPipelineContext context,
            ISequenceReader sequenceReader)
        {
            if (!sequenceReader.CanHandleSequence(context.Envelope))
                return false;

            ((RawInboundEnvelope)context.Envelope).Sequence =
                await sequenceReader.HandleSequence(context.Envelope).ConfigureAwait(false);

            return true;
        }

        private static void StartProcessingThread(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            // TODO: Handle transaction / exceptions
            Task.Factory.StartNew(
                async () => await next(context).ConfigureAwait(false),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
    }
}
