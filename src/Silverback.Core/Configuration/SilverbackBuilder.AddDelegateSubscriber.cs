// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Configuration;

/// <content>
///     Implements the <c>AddDelegateSubscriber</c> methods.
/// </content>
public partial class SilverbackBuilder
{
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Action<TMessage> handler, DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Func<TMessage, Task> handler, DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    public SilverbackBuilder AddDelegateSubscriber<TMessage>(Func<TMessage, ValueTask> handler, DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, TResult>(
        Func<TMessage, TResult> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, TResult>(
        Func<TMessage, Task<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, TResult>(
        Func<TMessage, ValueTask<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        Func<TMessage, T2, ValueTask> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, TResult>(
        Func<TMessage, T2, TResult> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, TResult>(
        Func<TMessage, T2, Task<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    /// <summary>
    ///     Subscribes the specified delegate to the messages being published into the bus.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be handled.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of an additional service to be resolved and passed to the method.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, TResult>(
        Func<TMessage, T2, ValueTask<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        Func<TMessage, T2, T3, ValueTask> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, TResult>(
        Func<TMessage, T2, T3, TResult> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, TResult>(
        Func<TMessage, T2, T3, Task<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, TResult>(
        Func<TMessage, T2, T3, ValueTask<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
        Func<TMessage, T2, T3, T4, ValueTask> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4, TResult>(
        Func<TMessage, T2, T3, T4, TResult> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4, TResult>(
        Func<TMessage, T2, T3, T4, Task<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

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
    /// <typeparam name="TResult">
    ///     The type of the result returned by the handler.
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
    public SilverbackBuilder AddDelegateSubscriber<TMessage, T2, T3, T4, TResult>(
        Func<TMessage, T2, T3, T4, ValueTask<TResult>> handler,
        DelegateSubscriptionOptions? options = null) =>
        AddSubscriber(DelegateSubscriber.Create(handler), MapTypeSubscriptionOptions(options));

    private static TypeSubscriptionOptions MapTypeSubscriptionOptions(DelegateSubscriptionOptions? options) =>
        options == null
            ? new TypeSubscriptionOptions()
            : new TypeSubscriptionOptions
            {
                Filters = options.Filters,
                IsExclusive = options.IsExclusive,
                AutoSubscribeAllPublicMethods = true
            };

    private SilverbackBuilder AddSubscriber(object subscriber, TypeSubscriptionOptions options) =>
        AddSingletonSubscriber(subscriber.GetType(), subscriber, options);
}
