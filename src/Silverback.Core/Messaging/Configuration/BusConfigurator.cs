// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public class BusConfigurator
    {
        private readonly BusOptions _busOptions;

        public BusConfigurator(BusOptions busOptions, IServiceProvider serviceProvider)
        {
            _busOptions = busOptions;
            ServiceProvider = serviceProvider;
        }

        internal IServiceProvider ServiceProvider { get; }

        #region HandleMessagesOfType

        /// <summary>
        ///     Configures the type <typeparamref name="TMessage" /> to be recognized
        ///     as a message to enable features like automatic republishing.
        /// </summary>
        /// <typeparam name="TMessage">The (base) message type.</typeparam>
        /// <returns></returns>
        public BusConfigurator HandleMessagesOfType<TMessage>() =>
            HandleMessagesOfType(typeof(TMessage));

        /// <summary>
        ///     Configures the specified type to be recognized
        ///     as a message to enable features like automatic republishing.
        /// </summary>
        /// <param name="messageType">The (base) message type.</param>
        /// <returns></returns>
        public BusConfigurator HandleMessagesOfType(Type messageType)
        {
            if (!_busOptions.MessageTypes.Contains(messageType))
                _busOptions.MessageTypes.Add(messageType);

            return this;
        }

        #endregion

        #region Subscribe (delegate)

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe(Delegate handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(Action<TMessage> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(Func<TMessage, Task> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(Func<TMessage, object> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<TMessage, Task<object>> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Action<IEnumerable<TMessage>> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<IReadOnlyCollection<TMessage>, Task> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<IReadOnlyCollection<TMessage>, object> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<IReadOnlyCollection<TMessage>, Task<object>> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        #endregion

        #region Subscribe (delegate w/ service provider)

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Action<TMessage, IServiceProvider> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<TMessage, IServiceProvider, object> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Action<IEnumerable<TMessage>, IServiceProvider> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be handled.</typeparam>
        /// <param name="handler">The message handler delegate.</param>
        /// <param name="options">A <see cref="SubscriptionOptions" /> instance specifying parallelism options.</param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TMessage>(
            Func<IReadOnlyCollection<TMessage>, IServiceProvider, object> handler,
            SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        #endregion

        #region Subscribe (automatic / annotation based)

        /// <summary>
        ///     Configures the type <typeparamref name="TSubscriber" /> to be used
        ///     to resolve the subscribers.
        /// </summary>
        /// <typeparam name="TSubscriber">The type to be used to resolve the subscribers instances.</typeparam>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods in
        ///     the resolved types have to be automatically subscribed, without relying on the <see cref="SubscribeAttribute" />.
        /// </param>
        /// <returns></returns>
        public BusConfigurator Subscribe<TSubscriber>(bool autoSubscribeAllPublicMethods = true) =>
            Subscribe(typeof(TSubscriber), autoSubscribeAllPublicMethods);

        /// <summary>
        ///     Configures the type <paramref name="subscriberType" /> to be used
        ///     to resolve the subscribers.
        /// </summary>
        /// <param name="subscriberType">The type to be used to resolve the subscribers instances.</param>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods in
        ///     the resolved types have to be automatically subscribed, without relying on the <see cref="SubscribeAttribute" />.
        /// </param>
        /// <returns></returns>
        public BusConfigurator Subscribe(Type subscriberType, bool autoSubscribeAllPublicMethods = true)
        {
            var previousSubscription = _busOptions.Subscriptions.OfType<TypeSubscription>()
                .FirstOrDefault(sub => sub.SubscribedType == subscriberType);

            if (previousSubscription != null)
                _busOptions.Subscriptions.Remove(previousSubscription);

            _busOptions.Subscriptions.Add(new TypeSubscription(subscriberType, autoSubscribeAllPublicMethods));
            return this;
        }

        #endregion

        #region ScanSubscribers

        /// <summary>
        ///     Resolves all the subscribers and build the types cache to speed-up the first
        ///     publish.
        /// </summary>
        /// <returns></returns>
        public BusConfigurator ScanSubscribers()
        {
            using var scope = ServiceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<SubscribedMethodsLoader>().GetSubscribedMethods();
            return this;
        }

        #endregion
    }
}