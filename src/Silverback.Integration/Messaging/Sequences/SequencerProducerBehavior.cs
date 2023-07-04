// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Uses the available implementations of <see cref="ISequenceWriter" /> to set the proper headers and
    ///     split the published message or messages set to create the sequences.
    /// </summary>
    /// <remarks>
    ///     A sequence is a set of messages that are handled as a single unit of work. A sequence could be used to
    ///     group all chunks belonging to the same source message, all messages belonging to the same data set or
    ///     to implement batch processing.
    /// </remarks>
    public class SequencerProducerBehavior : IProducerBehavior
    {
        private readonly IReadOnlyCollection<ISequenceWriter> _sequenceWriters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerProducerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceWriters">
        ///     The <see cref="ISequenceWriter" /> implementations to be used.
        /// </param>
        public SequencerProducerBehavior(IEnumerable<ISequenceWriter> sequenceWriters)
        {
            _sequenceWriters = sequenceWriters.ToList();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Sequencer;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler next,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            foreach (var sequenceWriter in _sequenceWriters)
            {
                if (!sequenceWriter.CanHandle(context.Envelope))
                    continue;

                if (cancellationToken.IsCancellationRequested)
                    break;

                var envelopesEnumerable = sequenceWriter.ProcessMessageAsync(context.Envelope);

                await foreach (var envelope in envelopesEnumerable.ConfigureAwait(false))
                {
                    var newContext = new ProducerPipelineContext(envelope, context.Producer, context.ServiceProvider);
                    await next(newContext, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
