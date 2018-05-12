using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <inheritdoc />
    public class KafkaOutboudAdapter : IOutboundAdapter
    {
        /// <inheritdoc />
        void IOutboundAdapter.Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
        {
            producer.Produce(Envelope.Create(message));
        }
    }
}
