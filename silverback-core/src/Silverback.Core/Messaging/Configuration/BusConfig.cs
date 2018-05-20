using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Exposes a fluent API to configure the <see cref="IBus"/>.
    /// </summary>
    public class BusConfig
    {
        internal IBus Bus { get; }
        internal ITypeFactory GetTypeFactory() => _typeFactory;

        private ITypeFactory _typeFactory;

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
        /// Creates a new bus of the specified type.
        /// </summary>
        /// <returns></returns>
        public static TBus Create<TBus>(Action<BusConfig> config)
            where TBus : IBus, new()
        {
            var bus = new TBus();
            config(new BusConfig(bus));
            return bus;
        }

        #endregion

        #region WithFactory


        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory method used to instanciate a single instance.</param>
        /// <returns></returns>
        public BusConfig WithFactory(Func<Type, object> singleInstanceFactory)
        {
            _typeFactory = new GenericTypeFactory(singleInstanceFactory);
            return this;
        }

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="multiInstancesFactory">TThe factory method used to instanciate multiple instances of a type.</param>
        /// <returns></returns>
        public BusConfig WithFactory(Func<Type, IEnumerable<object>> multiInstancesFactory)
        {
            _typeFactory = new GenericTypeFactory(multiInstancesFactory);
            return this;
        }

        /// <summary>
        /// Set the function to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="singleInstanceFactory">The factory method used to instanciate a single instance.</param>
        /// <param name="multiInstancesFactory">TThe factory method used to instanciate multiple instances of a type.</param>
        /// <returns></returns>
        public BusConfig WithFactory(Func<Type, object> singleInstanceFactory, Func<Type, IEnumerable<object>> multiInstancesFactory)
        {
            _typeFactory = new GenericTypeFactory(singleInstanceFactory, multiInstancesFactory);
            return this;
        }

        /// <summary>
        /// Set the <see cref="ITypeFactory" /> to be used to dinamically instantiate the types needed to handle the messages.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public BusConfig WithFactory(ITypeFactory factory)
        {
            _typeFactory = factory;
            return this;
        }

        /// <summary>
        /// Setup a factory that uses reflection to instanciate the subscribers.
        /// All types must have a parameterless constructor.
        /// </summary>
        /// <returns></returns>
        public BusConfig WithDefaultFactory()
            => WithFactory(t => Activator.CreateInstance(t));

        #endregion

        #region Subscribe

        /// <summary>
        /// Automatically subscribe all instances of <see cref="ISubscriber" /> that are resolved by the 
        /// configured <see cref="ITypeFactory"/>.
        /// This is the same as calling <code>Subscribe&lt;ISubscriber&gt;</code>.
        /// </summary>
        /// <returns></returns>
        public BusConfig AutoSubscribe()
        {
            Subscribe<ISubscriber>();
            return this;
        }

        /// <summary>
        /// Subscribes an instance of <see cref="ISubscriber" /> to the messages sent through this bus.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        /// <returns></returns>
        public BusConfig Subscribe(ISubscriber subscriber)
        {
            Bus.Subscribe(subscriber);
            return this;
        }

        /// <summary>
        /// Subscribes an <see cref="ISubscriber" /> using a <see cref="SubscriberFactory{TSubscriber}" />.
        /// </summary>
        /// <typeparam name="TSubscriber">Type of the <see cref="ISubscriber" /> to subscribe.</typeparam>
        /// <returns></returns>
        public BusConfig Subscribe<TSubscriber>()
            where TSubscriber : ISubscriber
        {
            Bus.Subscribe(new SubscriberFactory<TSubscriber>(GetTypeFactory));
            return this;
        }

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
        /// </summary>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe(Action<IMessage> handler, Func<IMessage, bool> filter = null)
            => Subscribe<IMessage>(handler, filter);

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            Bus.Subscribe(new GenericSubscriber<TMessage>(handler, filter));
            return this;
        }

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
        /// </summary>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe(Func<IMessage, Task> handler, Func<IMessage, bool> filter = null)
            => Subscribe<IMessage>(handler, filter);

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public BusConfig Subscribe<TMessage>(Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            Bus.Subscribe(new GenericAsyncSubscriber<TMessage>(handler, filter));
            return this;
        }
        #endregion

        #region ConfigureUsing

        /// <summary>
        /// Apply the specified <see cref="IConfigurator"/>.
        /// </summary>
        /// <typeparam name="TConfig">The type of the <see cref="IConfigurator"/>.</typeparam>
        /// <returns></returns>
        public BusConfig ConfigureUsing<TConfig>()
            where TConfig : IConfigurator, new()
        {
            new TConfig().Configure(this);
            return this;
        }

        #endregion
    }
}
