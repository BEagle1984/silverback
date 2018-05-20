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
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
    /// <seealso cref="Silverback.Messaging.Subscribers.ISubscriber" />
    public class OutboundSubscriber<TMessage, TAdapter> : SubscriberBase<TMessage>
        where TMessage : IIntegrationMessage
        where TAdapter : IOutboundAdapter
    {
        private readonly Func<ITypeFactory> _typeFactoryProvider;

        private readonly IEndpoint _endpoint;
        private readonly IProducer _producer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboundSubscriber{TMessage, TAdapter}" /> class.
        /// </summary>
        /// <param name="typeFactoryProvider">The method used to retrieve the <see cref="ITypeFactory" /> that will be used to get an <see cref="IOutboundAdapter" /> instance to relay each received message.</param>
        /// <param name="broker">The broker to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        public OutboundSubscriber(Func<ITypeFactory> typeFactoryProvider, IBroker broker, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            :base(filter)
        {
            _typeFactoryProvider = typeFactoryProvider ?? throw new ArgumentNullException(nameof(typeFactoryProvider));
            _producer = broker?.GetProducer(endpoint);
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        /// <summary>
        /// Gets a new adapter instance.
        /// </summary>
        /// <returns></returns>
        private TAdapter GetAdapter()
        {
            var adapter = _typeFactoryProvider().GetInstance<TAdapter>();

            if (adapter == null)
                throw new InvalidOperationException($"Couldn't instantiate adapter of type {typeof(TAdapter)}.");

            return adapter;
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public override void Handle(TMessage message)
            => GetAdapter().Relay(message, _producer, _endpoint);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" /> asynchronously.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        public override Task HandleAsync(TMessage message)
            => GetAdapter().RelayAsync(message, _producer, _endpoint);
    }
}
