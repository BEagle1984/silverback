// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Starts an <see cref="Activity" /> with the tracing information from the message headers.
    /// </summary>
    public class ActivityConsumerBehavior : IConsumerBehavior, ISorted
    {
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);
            try
            {
                activity.InitFromMessageHeaders(context.Envelopes.Single().Headers);
                activity.Start();
                await next(context, serviceProvider);
            }
            finally
            {
                activity.Stop();
            }
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;
    }
}