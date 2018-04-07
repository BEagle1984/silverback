using System;
using System.Reactive.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Exposes a fluent API to configure the <see cref="IBus"/>.
    /// </summary>
    public class BusConfig
    {
        internal IBus Bus { get; }
        internal ITypeFactory TypeFactory { get; private set; } // TODO: Change this into a function to allow WithFactory to be called at any time during configuration

        /// <summary>
        /// Initializes a new instance of the <see cref="BusConfig"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        internal BusConfig(IBus bus)
        {
            Bus = bus;
        }

        #region Create

        /// <summary>
        /// Creates a new <see cref="Bus"/>.
        /// </summary>
        /// <returns></returns>
        public static Bus Create(Action<BusConfig> config)
        {
            var bus = new Bus();
            config(new BusConfig(bus));
            return bus;
        }

        #endregion

        #region WithMessageHandlerProvider

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public BusConfig WithFactory(Func<Type, object> factory)
        {
            TypeFactory = new GenericTypeFactory(factory);
            return this;
        }

        /// <summary>
        /// Set the <see cref="ITypeFactory" /> to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public BusConfig WithFactory(ITypeFactory factory)
        {
            TypeFactory = factory;
            return this;
        }

        #endregion

        #region Subscribe

        /// <summary>
        /// Subscribes a <see cref="Subscriber" /> derived type to the messages of the specified type <see cref="TMessage" />.
        /// </summary>
        /// <param name="subscriberFactory">The subscriber factory method.</param>
        /// <returns></returns>
        public BusConfig Subscribe(Func<IObservable<IMessage>, Subscriber> subscriberFactory)
        {
            Bus.Subscribe(subscriberFactory);
            return this;
        }

        /// <summary>
        /// Subscribes an <see cref="IMessageHandler" /> using the <see cref="DefaultSubscriber" />.
        /// </summary>
        /// <param name="handlerType">Type of the <see cref="IMessageHandler" /> to be used to handle the messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe(Type handlerType)
        {
            Bus.Subscribe(messages => new DefaultSubscriber(messages, TypeFactory, handlerType));
            return this;
        }

        /// <summary>
        /// Subscribes an <see cref="IMessageHandler" /> using the <see cref="DefaultSubscriber" />.
        /// </summary>
        /// <typeparam name="THandler">Type of the <see cref="IMessageHandler" /> to be used to handle the messages.</typeparam>
        /// <returns></returns>
        public BusConfig Subscribe<THandler>()
            where THandler : IMessageHandler
        {
            Bus.Subscribe(messages => new DefaultSubscriber(messages, TypeFactory, typeof(THandler)));
            return this;
        }

        /// <summary>
        /// Subscribes an action method using the <see cref="DefaultSubscriber" />.
        /// </summary>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe(Action<IMessage> handler, Func<IMessage, bool> filter = null)
            => Subscribe<IMessage>(handler, filter);

        /// <summary>
        /// Subscribes an action method using the <see cref="DefaultSubscriber" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            Bus.Subscribe(messages => new DefaultSubscriber(
                messages,
                new GenericTypeFactory(_ => new GenericMessageHandler<TMessage>(handler, filter)),
                typeof(GenericMessageHandler<IMessage>)));
            return this;
        }

        #endregion

    }
}
