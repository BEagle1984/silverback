// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class RabbitBroker : Broker<RabbitEndpoint>
    {
        public RabbitBroker(IEnumerable<IBrokerBehavior> behaviors, ILoggerFactory loggerFactory) 
            : base(behaviors, loggerFactory)
        {
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors)
        {
            throw new System.NotImplementedException();
        }

        protected override Consumer InstantiateConsumer(IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
        {
            throw new System.NotImplementedException();
        }

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            throw new System.NotImplementedException();
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            throw new System.NotImplementedException();
        }
    }
}