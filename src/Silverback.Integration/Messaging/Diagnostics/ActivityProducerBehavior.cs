// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Starts an <see cref="Activity" /> and adds the tracing information to the message headers.
    /// </summary>
    public class ActivityProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Activity;

        /// <inheritdoc cref="IProducerBehavior.Handle" />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageProducing);

            try
            {
                activity.Start();
                activity.SetMessageHeaders(context.Envelope.Headers);
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
