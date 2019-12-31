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
        public async Task Handle(RawBrokerMessage message, RawBrokerMessageHandler next)
        {
            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageProducing);
            try
            {
                activity.Start();
                activity.SetMessageHeaders(message.Headers);
                await next(message);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}