using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.<br/>
    /// This is the simplest implementation and it doesn't prevent duplicated processing of the same message. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IInboundAdapter" />
    public class SimpleInboundAdapter : IInboundAdapter
    {
        /// <summary>
        /// The underlying bus.
        /// </summary>
        protected IBus Bus;

        /// <summary>
        /// The error policy
        /// </summary>
        protected IErrorPolicy ErrorPolicy;

        private IConsumer _consumer;

        /// <summary>
        /// Initializes the <see cref="T:Silverback.Messaging.Adapters.IInboundAdapter" />.
        /// </summary>
        /// <param name="bus">The internal <see cref="IBus" /> where the messages have to be relayed.</param>
        /// <param name="endpoint">The endpoint this adapter has to connect to.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        public void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            ErrorPolicy = errorPolicy ?? new NoErrorPolicy();
            ErrorPolicy.Init(bus);

            Connect(bus.GetBroker(endpoint.BrokerName), endpoint);
        }

        /// <summary>
        /// Implements the logic to connect and start listening to the specified endpoint.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <exception cref="InvalidOperationException">Connect was called twice.</exception>
        protected virtual void Connect(IBroker broker, IEndpoint endpoint)
        {
            if (_consumer != null)
                throw new InvalidOperationException("Connect was called twice.");

            _consumer = broker.GetConsumer(endpoint);

            _consumer.Received += OnMessageReceived;
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="envelope">The envelope containing the received message.</param>
        protected virtual void OnMessageReceived(object sender, IEnvelope envelope)
            => ErrorPolicy.TryHandleMessage(
                envelope,
                e => RelayMessage(e.Message));
        
        /// <summary>
        /// Relays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void RelayMessage(IIntegrationMessage message)
            => Bus.Publish(message);
    }
}