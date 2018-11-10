using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.
    /// </summary>
    /// <seealso cref="IInboundAdapter" />
    public class InboundAdapter : IInboundAdapter
    {
        private IConsumer _consumer;

        protected IBus Bus;
        protected IEndpoint Endpoint;
        protected IErrorPolicy ErrorPolicy;

        public virtual void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            ErrorPolicy = errorPolicy ?? new NoErrorPolicy();
            ErrorPolicy.Init(bus);

            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            Connect(bus.GetBroker(endpoint.BrokerName), endpoint);
        }

        protected virtual void Connect(IBroker broker, IEndpoint endpoint)
        {
            // TODO: Trace
            if (_consumer != null)
                throw new InvalidOperationException("Connect was called twice.");

            _consumer = broker.GetConsumer(endpoint);

            _consumer.Received += OnMessageReceived;
        }

        protected virtual void OnMessageReceived(object sender, IEnvelope envelope)
            => ErrorPolicy.TryHandleMessage(
                envelope,
                e => RelayMessage(e.Message));
        
        protected virtual void RelayMessage(IIntegrationMessage message)
            => Bus.Publish(message);            // TODO: Trace
    }
}