using System;
using System.Threading.Tasks;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// The standard subscriber used to attach the <see cref="IOutboundAdapter" />, suitable for most cases.
    /// In more advanced use cases, when a greater degree of flexibility is required, it is advised to create an ad-hoc implementation of <see cref="Subscriber{TMessage}" />.
    /// </summary>
    public class OutboundSubscriber<TMessage, TAdapter> : SubscriberBase<TMessage>
        where TMessage : IIntegrationMessage
        where TAdapter : IOutboundAdapter
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
            => GetAdapter().Relay(message, _producer, _endpoint);

        public override Task HandleAsync(TMessage message)
            => GetAdapter().RelayAsync(message, _producer, _endpoint);

        private TAdapter GetAdapter()
        {
            var adapter = _typeFactory.GetInstance<TAdapter>();

            if (adapter == null)
                throw new InvalidOperationException($"Couldn't instantiate adapter of type {typeof(TAdapter)}.");

            return adapter;
        }
    }
}
