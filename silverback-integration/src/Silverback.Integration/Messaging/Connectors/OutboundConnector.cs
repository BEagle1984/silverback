using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// The basic outbound connector that sends the messages directly through the message broker.
    /// </summary>
    public class OutboundConnector : OutboundConnectorBase
    {
        private readonly IBroker _broker;

        public OutboundConnector(IBroker broker, IOutboundRoutingConfiguration routing)
            : base(routing)
        {
            _broker = broker;
        }

        protected override Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint) =>
            _broker.GetProducer(destinationEndpoint).ProduceAsync(message);
    }
}