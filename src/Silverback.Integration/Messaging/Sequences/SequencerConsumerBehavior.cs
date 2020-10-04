// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

            var sequenceReader = _sequenceReaders.FirstOrDefault(reader => reader.CanHandle(context));

            if (sequenceReader == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            // Store the original envelope in case it gets replaced in the GetSequence method
            var originalEnvelope = context.Envelope;
            var sequence = sequenceReader.GetSequence(context, out var isNew);

            if (sequence != null)
            {
                ((RawInboundEnvelope)context.Envelope).Sequence = sequence;

                if (isNew)
                {
                    context.IsSequenceNew = true;

                    await next(context).ConfigureAwait(false);

                    CheckPrematureCompletion(context);
                }

                await sequence.AddAsync(originalEnvelope).ConfigureAwait(false);
            }
        }

        private void CheckPrematureCompletion(ConsumerPipelineContext context)
        {
            if (context.ProcessingTask == null || context.Sequence == null)
                return;

            Task.Run(
                async () =>
                {
                    try
                    {
                        await context.ProcessingTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        // Abort the uncompleted sequence if the processing task completes, to avoid unreleased locks.
                        if (!context.Sequence.IsComplete)
                            context.Sequence.AbortProcessing();
                    }
                });
        }
    }
}
