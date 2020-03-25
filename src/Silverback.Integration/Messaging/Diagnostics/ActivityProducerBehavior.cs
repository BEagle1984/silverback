// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    // TODO: Test
    public class ActivityProducerBehavior : IProducerBehavior
    {
        public async Task Handle(IRawOutboundEnvelope envelope, RawOutboundEnvelopeHandler next)
        {
            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageProducing);
            try
            {
                activity.Start();
                activity.SetMessageHeaders(envelope.Headers);
                await next(envelope);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}