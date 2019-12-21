// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryProducer : Producer<InMemoryBroker, IEndpoint>
    {
        public InMemoryProducer(
            IBroker broker,
            IEndpoint endpoint,
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, behaviors, logger, messageLogger)
        {
        }

        protected override IOffset Produce(RawBrokerMessage message) => 
            Broker.GetTopic(Endpoint.Name).Publish(message.RawContent, message.Headers);

        protected override Task<IOffset> ProduceAsync(RawBrokerMessage message) => 
            Broker.GetTopic(Endpoint.Name).PublishAsync(message.RawContent, message.Headers);
    }
}