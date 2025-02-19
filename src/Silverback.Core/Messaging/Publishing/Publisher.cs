// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <inheritdoc cref="IPublisher" />
internal class Publisher : IPublisher
{
    private readonly ISilverbackLogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly IBehaviorsProvider _behaviorsProvider;

    private readonly SubscribedMethodsCache _subscribedMethodsCache;

    private readonly bool _isInServiceScope;

    private ISilverbackContext? _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Publisher" /> class.
    /// </summary>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBehaviorsProvider" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> instance to be used to resolve the subscribers.
    /// </param>
    /// <param name="rootServiceProvider">
    ///     The root <see cref="IServiceProvider" /> instance to be used to compare with the <paramref name="serviceProvider" /> and determine
    ///     if the publisher is used in a service scope.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public Publisher(
        IBehaviorsProvider behaviorsProvider,
        IServiceProvider serviceProvider,
        RootServiceProvider rootServiceProvider,
        ISilverbackLogger<Publisher> logger)
    {
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        _isInServiceScope = serviceProvider != rootServiceProvider.ServiceProvider;

        _subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();
    }

    /// <inheritdoc cref="IPublisher.Context" />
    public ISilverbackContext Context => _context ??= _isInServiceScope
        ? _serviceProvider.GetRequiredService<ISilverbackContext>() // use the scoped context only if the publisher is used in a service scope
        : new SilverbackContext(_serviceProvider); // otherwise create a transient one, but some stuff might not work as expected

    /// <inheritdoc cref="IPublisher.Publish(object, bool)" />
    public void Publish(object message, bool throwIfUnhandled = false) =>
        PublishAsync(message, throwIfUnhandled, ExecutionFlow.Sync, CancellationToken.None).SafeWait();

    /// <inheritdoc cref="IPublisher.Publish{TResult}(object, bool)" />
    public IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled = false) =>
        CastResults<TResult>(PublishAsync(message, throwIfUnhandled, ExecutionFlow.Sync, CancellationToken.None).SafeWait()).ToList();

    /// <inheritdoc cref="IPublisher.PublishAsync(object, CancellationToken)" />
    public Task PublishAsync(object message, CancellationToken cancellationToken = default) =>
        PublishAsync(message, false, cancellationToken);

    /// <inheritdoc cref="IPublisher.PublishAsync(object, bool, CancellationToken)" />
    public async Task PublishAsync(object message, bool throwIfUnhandled, CancellationToken cancellationToken = default) =>
        await PublishAsync(message, throwIfUnhandled, ExecutionFlow.Async, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, CancellationToken)" />
    public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, CancellationToken cancellationToken = default) =>
        PublishAsync<TResult>(message, false, cancellationToken);

    /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, bool, CancellationToken)" />
    public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(
        object message,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        CastResults<TResult>(await PublishAsync(message, throwIfUnhandled, ExecutionFlow.Async, cancellationToken).ConfigureAwait(false)).ToList();

    private ValueTask<IReadOnlyCollection<object?>> ExecuteBehaviorsPipelineAsync(
        Stack<IBehavior> behaviors,
        object message,
        bool throwIfUnhandled,
        Func<object, bool, ExecutionFlow, CancellationToken, ValueTask<IReadOnlyCollection<object?>>> finalAction,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        if (!behaviors.TryPop(out IBehavior? nextBehavior))
            return finalAction(message, throwIfUnhandled, executionFlow, cancellationToken);

        return nextBehavior.HandleAsync(this, message, NextAsync, cancellationToken);

        ValueTask<IReadOnlyCollection<object?>> NextAsync(object nextMessage) =>
            ExecuteBehaviorsPipelineAsync(behaviors, nextMessage, throwIfUnhandled, finalAction, executionFlow, cancellationToken);
    }

    private ValueTask<IReadOnlyCollection<object?>> PublishAsync(
        object message,
        bool throwIfUnhandled,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        Check.NotNull(message, nameof(message));

        return ExecuteBehaviorsPipelineAsync(
            _behaviorsProvider.CreateStack(),
            message,
            throwIfUnhandled,
            PublishCoreAsync,
            executionFlow,
            cancellationToken);
    }

    private async ValueTask<IReadOnlyCollection<object?>> PublishCoreAsync(
        object message,
        bool throwIfUnhandled,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MethodInvocationResult> resultsCollection =
            await InvokeSubscribedMethodsAsync(message, executionFlow, cancellationToken).ConfigureAwait(false);

        bool handled = resultsCollection.Any(invocationResult => invocationResult.WasInvoked);

        if (!handled && throwIfUnhandled)
            throw new UnhandledMessageException(message);

        return resultsCollection.Select(invocationResult => invocationResult.ReturnValue).ToList();
    }

    private IEnumerable<TResult> CastResults<TResult>(IReadOnlyCollection<object?> results)
    {
        foreach (object? result in results)
        {
            if (result is TResult castResult)
                yield return castResult;
            else
                _logger.LogSubscriberResultDiscarded(result?.GetType().FullName, typeof(TResult).FullName!);
        }
    }

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeSubscribedMethodsAsync(
        object message,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken) =>
        (await InvokeExclusiveMethodsAsync(message, executionFlow, cancellationToken).ConfigureAwait(false))
        .Union(await InvokeNonExclusiveMethodsAsync(message, executionFlow, cancellationToken).ConfigureAwait(false))
        .ToList();

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeExclusiveMethodsAsync(
        object message,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SubscribedMethod> methods = _subscribedMethodsCache.GetExclusiveMethods(message, _serviceProvider);

        if (methods.Count == 0)
            return [];

        if (methods.Count == 1)
            return [await InvokeAsync(methods[0]).ConfigureAwait(false)];

        return (await methods
                .SelectAsync(InvokeAsync)
                .ConfigureAwait(false))
            .ToList();

        ValueTask<MethodInvocationResult> InvokeAsync(SubscribedMethod method) =>
            SubscribedMethodInvoker.InvokeAsync(this, method, message, _serviceProvider, executionFlow, cancellationToken);
    }

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeNonExclusiveMethodsAsync(
        object message,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SubscribedMethod> methods = _subscribedMethodsCache.GetNonExclusiveMethods(message, _serviceProvider);

        if (methods.Count == 0)
            return [];

        if (methods.Count == 1)
            return [await InvokeAsync(methods[0]).ConfigureAwait(false)];

        return (await methods
                .ParallelSelectAsync(InvokeAsync)
                .ConfigureAwait(false))
            .ToList();

        ValueTask<MethodInvocationResult> InvokeAsync(SubscribedMethod method) =>
            SubscribedMethodInvoker.InvokeAsync(this, method, message, _serviceProvider, executionFlow, cancellationToken);
    }
}
