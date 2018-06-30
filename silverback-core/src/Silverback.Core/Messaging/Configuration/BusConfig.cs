// TODO: DELETE

//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Silverback.Messaging.Messages;
//using Silverback.Messaging.Subscribers;

//namespace Silverback.Messaging.Configuration
//{
//    /// <summary>
//    /// Exposes a fluent API to configure the <see cref="IBus"/>.
//    /// </summary>
//    public class BusConfig
//    {
//        internal IBus Bus { get; }
//        internal ITypeFactory GetTypeFactory() => _typeFactory;

//        private ITypeFactory _typeFactory;
//        private ILoggerFactory _loggerFactory;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="BusConfig"/> class.
//        /// </summary>
//        /// <param name="bus">The bus.</param>
//        internal BusConfig(IBus bus)
//        {
//            Bus = bus;
//        }

//        #region Create

//        /// <summary>
//        /// Creates a new bus of the specified type.
//        /// </summary>
//        /// <returns></returns>
//        public static TBus Create<TBus>(Action<BusConfig> config)
//            where TBus : IBus, new()
//        {
//            var bus = new TBus();
//            config(new BusConfig(bus));
//            return bus;
//        }

//        #endregion

    

//        //#region Subscribe

//        ///// <summary>
//        ///// Automatically subscribe all instances of <see cref="ISubscriber" /> that are resolved by the 
//        ///// configured <see cref="ITypeFactory"/>.
//        ///// This is the same as calling <code>Subscribe&lt;ISubscriber&gt;</code>.
//        ///// </summary>
//        ///// <returns></returns>
//        //public BusConfig AutoSubscribe()
//        //{
//        //    Subscribe<ISubscriber>();
//        //    return this;
//        //}

//        ///// <summary>
//        ///// Subscribes an instance of <see cref="ISubscriber" /> to the messages sent through this bus.
//        ///// </summary>
//        ///// <param name="subscriber">The subscriber.</param>
//        ///// <returns></returns>
//        //public BusConfig Subscribe(ISubscriber subscriber)
//        //{
//        //    Bus.Subscribe(subscriber);
//        //    return this;
//        //}

//        ///// <summary>
//        ///// Subscribes an <see cref="ISubscriber" /> using a <see cref="SubscriberFactory{TSubscriber}" />.
//        ///// </summary>
//        ///// <typeparam name="TSubscriber">Type of the <see cref="ISubscriber" /> to subscribe.</typeparam>
//        ///// <returns></returns>
//        //public BusConfig Subscribe<TSubscriber>()
//        //    where TSubscriber : ISubscriber
//        //{
//        //    Bus.Subscribe(new SubscriberFactory<TSubscriber>(GetTypeFactory));
//        //    return this;
//        //}

//        ///// <summary>
//        ///// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
//        ///// </summary>
//        ///// <param name="handler">The message handler method.</param>
//        ///// <param name="filter">An optional filter to be applied to the published messages.</param>
//        ///// <returns></returns>
//        //public BusConfig Subscribe(Action<IMessage> handler, Func<IMessage, bool> filter = null)
//        //    => Subscribe<IMessage>(handler, filter);

//        ///// <summary>
//        ///// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
//        ///// </summary>
//        ///// <typeparam name="TMessage">The type of the messages.</typeparam>
//        ///// <param name="handler">The message handler method.</param>
//        ///// <param name="filter">An optional filter to be applied to the published messages.</param>
//        ///// <returns></returns>
//        //public BusConfig Subscribe<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
//        //    where TMessage : IMessage
//        //{
//        //    Bus.Subscribe(new GenericSubscriber<TMessage>(handler, filter));
//        //    return this;
//        //}

//        ///// <summary>
//        ///// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
//        ///// </summary>
//        ///// <param name="handler">The message handler method.</param>
//        ///// <param name="filter">An optional filter to be applied to the published messages.</param>
//        ///// <returns></returns>
//        //public BusConfig Subscribe(Func<IMessage, Task> handler, Func<IMessage, bool> filter = null)
//        //    => Subscribe<IMessage>(handler, filter);

//        ///// <summary>
//        ///// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
//        ///// </summary>
//        ///// <typeparam name="TMessage">The type of the messages.</typeparam>
//        ///// <param name="handler">The message handler method.</param>
//        ///// <param name="filter">An optional filter to be applied to the published messages.</param>
//        ///// <returns></returns>
//        //public BusConfig Subscribe<TMessage>(Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
//        //    where TMessage : IMessage
//        //{
//        //    Bus.Subscribe(new GenericAsyncSubscriber<TMessage>(handler, filter));
//        //    return this;
//        //}
//        //#endregion

//        //#region ConfigureUsing

//        ///// <summary>
//        ///// Apply the specified <see cref="IConfigurator"/>.
//        ///// </summary>
//        ///// <typeparam name="TConfig">The type of the <see cref="IConfigurator"/>.</typeparam>
//        ///// <returns></returns>
//        //public BusConfig ConfigureUsing<TConfig>()
//        //    where TConfig : IConfigurator, new()
//        //{
//        //    new TConfig().Configure(this);
//        //    return this;
//        //}

//        //#endregion
//    }
//}
