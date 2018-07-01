using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains a set of extention methods useful to setup the <see cref="IBus"/>.
    /// </summary>
    public static class BusExtensions
    {
        private const string ItemsKeyPrefix = "Silverback.Configuration.";

        #region Type Factory (IoC)

        /// <summary>
        /// Sets the type factory.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="typeFactory">The type factory.</param>
        /// <returns></returns>
        internal static ITypeFactory SetTypeFactory(this IBus bus, ITypeFactory typeFactory)
            => (ITypeFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ITypeFactory", typeFactory, (_, __) => typeFactory);

        /// <summary>
        /// Gets the currently configured type factory.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static ITypeFactory GetTypeFactory(this IBus bus)
        {
            if (bus.Items.TryGetValue(ItemsKeyPrefix + "ITypeFactory", out var loggerFactory))
                return loggerFactory as ITypeFactory;

            return null;
        }

        #endregion

        #region Logger Factory

        /// <summary>
        /// Sets the logger factory.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns></returns>
        internal static ILoggerFactory SetLoggerFactory(this IBus bus, ILoggerFactory loggerFactory)
            => (ILoggerFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ILoggerFactory", loggerFactory, (_, __) => loggerFactory);

        /// <summary>
        /// Gets the currently configured logger factory.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static ILoggerFactory GetLoggerFactory(this IBus bus)
        {
            if (bus.Items.TryGetValue(ItemsKeyPrefix + "ILoggerFactory", out object loggerFactory))
                return loggerFactory as ILoggerFactory;

            return null;
        }

        #endregion

        #region Subscribe

        /// <summary>
        /// Automatically subscribe all instances of <see cref="ISubscriber" /> that are resolved by the
        /// configured <see cref="ITypeFactory" />.
        /// This is the same as calling <code>Subscribe&lt;ISubscriber&gt;</code>.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IBus AutoSubscribe(this IBus bus)
        {
            bus.Subscribe<ISubscriber>();
            return bus;
        }

        /// <summary>
        /// Subscribes an instance of <see cref="ISubscriber" /> to the messages sent through this bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="subscriber">The subscriber.</param>
        /// <returns></returns>
        public static IBus Subscribe(this IBus bus, ISubscriber subscriber)
        {
            bus.Subscribe(subscriber);
            return bus;
        }

        /// <summary>
        /// Subscribes an <see cref="ISubscriber" /> using a <see cref="SubscriberFactory{TSubscriber}" />.
        /// </summary>
        /// <typeparam name="TSubscriber">Type of the <see cref="ISubscriber" /> to subscribe.</typeparam>
        /// <returns></returns>
        public static IBus Subscribe<TSubscriber>(this IBus bus)
            where TSubscriber : ISubscriber
        {
            bus.Subscribe(new SubscriberFactory<TSubscriber>(bus.GetTypeFactory()));
            return bus;
        }

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static IBus Subscribe(this IBus bus, Action<IMessage> handler, Func<IMessage, bool> filter = null)
            => bus.Subscribe<IMessage>(handler, filter);

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericSubscriber{TMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static IBus Subscribe<TMessage>(this IBus bus, Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            bus.Subscribe(new GenericSubscriber<TMessage>(handler, filter));
            return bus;
        }

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static IBus Subscribe(this IBus bus, Func<IMessage, Task> handler, Func<IMessage, bool> filter = null)
            => bus.Subscribe<IMessage>(handler, filter);

        /// <summary>
        /// Subscribes an action method using a <see cref="GenericAsyncSubscriber{TMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="handler">The message handler method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static IBus Subscribe<TMessage>(this IBus bus, Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            bus.Subscribe(new GenericAsyncSubscriber<TMessage>(handler, filter));
            return bus;
        }
        #endregion

        #region ConfigureUsing

        /// <summary>
        /// Apply the specified <see cref="IConfigurator" />.
        /// </summary>
        /// <typeparam name="TConfig">The type of the <see cref="IConfigurator" />.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IBus ConfigureUsing<TConfig>(this IBus bus)
            where TConfig : IConfigurator, new()
        {
            new TConfig().Configure(bus);
            return bus;
        }

        #endregion
    }
}