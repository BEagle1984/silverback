// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddDelegateSubscriber</c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddDelegateSubscriberExtensions
    {
        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Delegate handler,
            SubscriptionOptions? options = null)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(handler, nameof(handler));
            options ??= new SubscriptionOptions();

            silverbackBuilder.BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return silverbackBuilder;
        }

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Action<TMessage> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, Task> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, CancellationToken, Task> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, CancellationToken, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, Task<object>> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, CancellationToken, Task<object>> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Action<IEnumerable<TMessage>> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, Task> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, CancellationToken, Task> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, CancellationToken, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, Task<object>> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, CancellationToken, Task<object>> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Action<TMessage, IServiceProvider> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, IServiceProvider, Task> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<TMessage, IServiceProvider, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Action<IEnumerable<TMessage>, IServiceProvider> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);

        /// <summary>
        ///     Subscribes the specified delegate to the messages being published into the bus.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be handled.
        /// </typeparam>
        /// <param name="handler">
        ///     The message handler delegate.
        /// </param>
        /// <param name="options">
        ///     The <see cref="SubscriptionOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddDelegateSubscriber<TMessage>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IEnumerable<TMessage>, IServiceProvider, object> handler,
            SubscriptionOptions? options = null) =>
            AddDelegateSubscriber(silverbackBuilder, (Delegate)handler, options);
    }
}
