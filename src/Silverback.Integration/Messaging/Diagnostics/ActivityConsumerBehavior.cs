// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    // TODO: Test
    public class ActivityConsumerBehavior : IConsumerBehavior
    {
        public async Task Handle(IRawInboundEnvelope envelope, RawInboundEnvelopeHandler next)
        {
            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);
            try
            {
                activity.InitFromMessageHeaders(envelope.Headers);
                activity.Start();
                await next(envelope);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}