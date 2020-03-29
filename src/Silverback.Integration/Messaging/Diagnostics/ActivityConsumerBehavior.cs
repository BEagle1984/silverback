// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    // TODO: Test
    public class ActivityConsumerBehavior : IConsumerBehavior, ISorted
    {
        public async Task Handle(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next)
        {
            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);
            try
            {
                activity.InitFromMessageHeaders(envelopes.Single().Headers);
                activity.Start();
                await next(envelopes, serviceProvider, consumer);
            }
            finally
            {
                activity.Stop();
            }
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;
    }
}