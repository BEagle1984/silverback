using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    public static class BusConfigurationExtensions
    {
        private const string ItemsKeyPrefix = "Silverback.Messaging.Configuration.";

        #region Type Factory (IoC)

        internal static ITypeFactory SetTypeFactory(this IBus bus, ITypeFactory typeFactory)
            => (ITypeFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ITypeFactory", typeFactory, (_, __) => typeFactory);

        internal static ITypeFactory GetTypeFactory(this IBus bus)
        {
            if (bus.Items.TryGetValue(ItemsKeyPrefix + "ITypeFactory", out var loggerFactory))
                return loggerFactory as ITypeFactory;

            return null;
        }

        #endregion

        #region Logger Factory

        internal static ILoggerFactory SetLoggerFactory(this IBus bus, ILoggerFactory loggerFactory)
            => (ILoggerFactory)bus.Items.AddOrUpdate(ItemsKeyPrefix + "ILoggerFactory", loggerFactory, (_, __) => loggerFactory);

        internal static ILoggerFactory GetLoggerFactory(this IBus bus)
        {
            if (bus.Items.TryGetValue(ItemsKeyPrefix + "ILoggerFactory", out object loggerFactory))
                return loggerFactory as ILoggerFactory;

            return null;
        }

        #endregion

        #region Subscribe

        public static IBus Subscribe(this IBus bus, Action<IMessage> handler, Func<IMessage, bool> filter = null)
            => bus.Subscribe<IMessage>(handler, filter);

        public static IBus Subscribe<TMessage>(this IBus bus, Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            bus.Subscribe(new GenericSubscriber<TMessage>(handler, filter));
            return bus;
        }

        public static IBus Subscribe(this IBus bus, Func<IMessage, Task> handler, Func<IMessage, bool> filter = null)
            => bus.Subscribe<IMessage>(handler, filter);

        public static IBus Subscribe<TMessage>(this IBus bus, Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            bus.Subscribe(new GenericAsyncSubscriber<TMessage>(handler, filter));
            return bus;
        }

        /// <summary>
        /// Subscribes all object of type <typeparamref name="TSubscriber"/> that are resolved by the configured <see cref="ITypeFactory"/>.
        /// The objects are resolved at every message and their life cycle is determined by the <see cref="ITypeFactory"/>.
        /// Use <see cref="SubscribeAttribute"/> to decorate the message handling methods.
        /// </summary>
        public static IBus Subscribe<TSubscriber>(this IBus bus)
        {
            bus.Subscribe(new SubscriberFactory<TSubscriber>(bus.GetTypeFactory()));
            return bus;
        }

        #endregion

        #region ConfigureUsing

        public static IBus ConfigureUsing<TConfig>(this IBus bus)
            where TConfig : IConfigurator, new()
        {
            new TConfig().Configure(bus);
            return bus;
        }

        #endregion
    }
}