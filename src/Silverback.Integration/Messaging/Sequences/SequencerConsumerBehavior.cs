// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="SequencerConsumerBehaviorBase" />
    public class SequencerConsumerBehavior : SequencerConsumerBehaviorBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        public SequencerConsumerBehavior(IEnumerable<ISequenceReader> sequenceReaders)
            : base(sequenceReaders.Where(reader => !reader.HandlesRawMessages))
        {
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public override int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer;

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.PublishSequenceAsync" />
        protected override Task PublishSequenceAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(next, nameof(next));

            return next(context);
        }
    }
}
