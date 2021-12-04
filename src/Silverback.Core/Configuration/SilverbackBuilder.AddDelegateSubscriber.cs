// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddDelegateSubscriber methods to the <see cref="SilverbackBuilder" />.
/// </content>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="SubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber(Delegate handler, SubscriptionOptions? options = null)
    {
        Check.NotNull(handler, nameof(handler));
        options ??= new SubscriptionOptions();

        BusOptions.Subscriptions.Add(new DelegateSubscription(handler, options));

        return this;
    }

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Action<TMessage> handler, SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Func<TMessage, Task> handler, SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, object> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, Task<object>> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Action<IEnumerable<TMessage>> handler, SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<IEnumerable<TMessage>, Task> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<IEnumerable<TMessage>, object> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<IEnumerable<TMessage>, Task<object>> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Action<TMessage, IServiceProvider> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, IServiceProvider, Task> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, IServiceProvider, object> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Action<IEnumerable<TMessage>, IServiceProvider> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<IEnumerable<TMessage>, IServiceProvider, object> handler,
        SubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);
}
