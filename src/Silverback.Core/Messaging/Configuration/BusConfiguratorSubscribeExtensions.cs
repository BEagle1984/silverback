// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c> Subscribe </c> methods to the <see cref="IBusConfigurator" />.
    /// </summary>
    public static class BusConfiguratorSubscribeExtensions
    {
        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe(
            this IBusConfigurator busConfigurator,
            Delegate handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Action<TMessage> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<TMessage, Task> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<TMessage, object> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<TMessage, Task<object>> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Action<IEnumerable<TMessage>> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<IEnumerable<TMessage>, Task> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<IEnumerable<TMessage>, object> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<IEnumerable<TMessage>, Task<object>> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Action<TMessage, IServiceProvider> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<TMessage, IServiceProvider, object> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Action<IEnumerable<TMessage>, IServiceProvider> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Subscribes the specified handler method to the messages being published into the bus.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler"> The message handler delegate. </param>
        /// <param name="options">
        ///     A <see cref="SubscriptionOptions" /> specifying parallelism options.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TMessage>(
            this IBusConfigurator busConfigurator,
            Func<IReadOnlyCollection<TMessage>, IServiceProvider, object> handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(handler, nameof(handler));

            busConfigurator.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return busConfigurator;
        }

        /// <summary>
        ///     Configures the type <typeparamref name="TSubscriber" /> to be used
        ///     to resolve the subscribers.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <typeparam name="TSubscriber">
        ///     The type to be used to resolve the subscribers.
        /// </typeparam>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods in
        ///     the resolved types have to be automatically subscribed, without relying on the
        ///     <see cref="SubscribeAttribute" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe<TSubscriber>(
            this IBusConfigurator busConfigurator,
            bool autoSubscribeAllPublicMethods = true) =>
            busConfigurator.Subscribe(typeof(TSubscriber), autoSubscribeAllPublicMethods);

        /// <summary>
        ///     Configures the type <paramref name="subscriberType" /> to be used
        ///     to resolve the subscribers.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to
        ///     be configured.
        /// </param>
        /// <param name="subscriberType">
        ///     The type to be used to resolve the subscribers.
        /// </param>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods in
        ///     the resolved types have to be automatically subscribed, without relying on the
        ///     <see cref="SubscribeAttribute" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator Subscribe(
            this IBusConfigurator busConfigurator,
            Type subscriberType,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(busConfigurator, nameof(busConfigurator));
            Check.NotNull(subscriberType, nameof(subscriberType));

            var previousSubscription = busConfigurator.BusOptions.Subscriptions.OfType<TypeSubscription>()
                .FirstOrDefault(sub => sub.SubscribedType == subscriberType);

            if (previousSubscription != null)
                busConfigurator.BusOptions.Subscriptions.Remove(previousSubscription);

            busConfigurator.BusOptions.Subscriptions.Add(
                new TypeSubscription(subscriberType, autoSubscribeAllPublicMethods));
            return busConfigurator;
        }
    }
}
