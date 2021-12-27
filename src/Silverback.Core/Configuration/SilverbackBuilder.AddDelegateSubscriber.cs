// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber(Delegate handler, DelegateSubscriptionOptions? options = null)
    {
        Check.NotNull(handler, nameof(handler));
        options ??= new DelegateSubscriptionOptions();

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
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Action<TMessage> handler,
        DelegateSubscriptionOptions? options = null) =>
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
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, Task> handler,
        DelegateSubscriptionOptions? options = null) =>
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
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(
        Func<TMessage, object> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2>(
        Action<TMessage, T2> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2>(
        Func<TMessage, T2, Task> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2>(
        Func<TMessage, T2, object> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3>(
        Action<TMessage, T2, T3> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3>(
        Func<TMessage, T2, T3, Task> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3>(
        Func<TMessage, T2, T3, object> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T4">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4>(
        Action<TMessage, T2, T3, T4> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T4">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4>(
        Func<TMessage, T2, T3, T4, Task> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T3">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="T4">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <param name="handler">
    ///     The message handler delegate.
    /// </param>
    /// <param name="options">
    ///     The <see cref="DelegateSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Documentation", "SA1625:Element documentation must not be copied and pasted", Justification = "Intentional")]
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4>(
        Func<TMessage, T2, T3, T4, object> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddDelegateSubscriber((Delegate)handler, options);
}
