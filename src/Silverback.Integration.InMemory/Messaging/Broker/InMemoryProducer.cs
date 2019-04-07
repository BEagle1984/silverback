// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryProducer : Producer<InMemoryBroker, IEndpoint>
    {
        public InMemoryProducer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
        }

        protected override void Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers) =>
            Broker.GetTopic(Endpoint.Name).Publish(serializedMessage, headers);

        protected override Task ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            Produce(message, serializedMessage, headers);
            return Task.CompletedTask;
        }
    }
}