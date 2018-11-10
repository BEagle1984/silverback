using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// The standard subscriber used to attach the <see cref="IOutboundConnector" />, suitable for most cases.
    /// In more advanced use cases, when a greater degree of flexibility is required, it is advised to create an ad-hoc implementation of <see cref="Subscriber{TMessage}" />.
    /// </summary>
    public class OutboundSubscriber<TMessage, TConnector> : SubscriberBase<TMessage>
        where TMessage : IIntegrationMessage
        where TConnector : IOutboundConnector
    {
        private readonly ITypeFactory _typeFactory;

        private readonly IEndpoint _endpoint;
        private readonly IProducer _producer;

        public OutboundSubscriber(ITypeFactory typeFactory, IBroker broker, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            : base(filter)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
            _producer = broker?.GetProducer(endpoint);
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public override void Handle(TMessage message)
            => GetConnector().Relay(message, _producer, _endpoint);

        public override Task HandleAsync(TMessage message)
            => GetConnector().RelayAsync(message, _producer, _endpoint);

        private TConnector GetConnector()
        {
            var connector = _typeFactory.GetInstance<TConnector>();

            if (connector == null)
                throw new InvalidOperationException($"Couldn't instantiate connector of type {typeof(TConnector)}.");

            return connector;
        }
    }
}
