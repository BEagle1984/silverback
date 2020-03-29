// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Starts an <see cref="Activity" /> and adds the tracing information to the message headers.
    /// </summary>
    public class ActivityProducerBehavior : IProducerBehavior
    {
        public async Task Handle(IOutboundEnvelope envelope, OutboundEnvelopeHandler next)
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