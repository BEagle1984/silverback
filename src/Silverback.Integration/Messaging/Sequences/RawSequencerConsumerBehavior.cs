﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="SequencerConsumerBehaviorBase" />
    public class RawSequencerConsumerBehavior : SequencerConsumerBehaviorBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RawSequencerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public RawSequencerConsumerBehavior(
            IEnumerable<ISequenceReader> sequenceReaders,
            ISilverbackIntegrationLogger<RawSequencerConsumerBehavior> logger)
            : base(sequenceReaders.Where(reader => reader.HandlesRawMessages), logger)
        {
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public override int SortIndex => BrokerBehaviorsSortIndexes.Consumer.RawSequencer;

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.PublishSequenceAsync" />
        protected override Task PublishSequenceAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var processingTask = Task.Run(async () => await next(context).ConfigureAwait(false));

            context.ProcessingTask ??= processingTask;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.AwaitOtherBehaviorIfNeededAsync" />
        [SuppressMessage("", "CA1031", Justification = "Catched in the sequence handling methods")]
        protected override async Task AwaitOtherBehaviorIfNeededAsync(ISequence sequence)
        {
            try
            {
                if (sequence is ISequenceImplementation sequenceImpl)
                    await sequenceImpl.SequencerBehaviorsTask.ConfigureAwait(false);
            }
            catch
            {
                // TODO: Ok to Swallow?
            }
        }
    }
}
