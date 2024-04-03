// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <inheritdoc cref="IPublisher" />
public class Publisher : IPublisher
{
    private readonly ISilverbackLogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly IBehaviorsProvider _behaviorsProvider;

    private readonly SubscribedMethodsCache _subscribedMethodsCache;

    private SilverbackContext? _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Publisher" /> class.
    /// </summary>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBehaviorsProvider" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> instance to be used to resolve the subscribers.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public Publisher(
        IBehaviorsProvider behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<Publisher> logger)
    {
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        _subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();
    }

    /// <inheritdoc cref="IPublisher.Context" />
    public SilverbackContext Context => _context ??= _serviceProvider.GetRequiredService<SilverbackContext>();

    /// <inheritdoc cref="IPublisher.Publish(object, bool)" />
    public void Publish(object message, bool throwIfUnhandled = false) =>
        PublishAsync(message, throwIfUnhandled, ExecutionFlow.Sync).SafeWait();

    /// <inheritdoc cref="IPublisher.Publish{TResult}(object, bool)" />
    public IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled = false) =>
        CastResults<TResult>(PublishAsync(message, throwIfUnhandled, ExecutionFlow.Sync).SafeWait()).ToList();

    /// <inheritdoc cref="IPublisher.PublishAsync(object, bool)" />
    public async ValueTask PublishAsync(object message, bool throwIfUnhandled = false) =>
        await PublishAsync(message, throwIfUnhandled, ExecutionFlow.Async).ConfigureAwait(false);

    /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, bool)" />
    public async ValueTask<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, bool throwIfUnhandled = false) =>
        CastResults<TResult>(await PublishAsync(message, throwIfUnhandled, ExecutionFlow.Async).ConfigureAwait(false)).ToList();

    private static ValueTask<IReadOnlyCollection<object?>> ExecuteBehaviorsPipelineAsync(
        Stack<IBehavior> behaviors,
        object message,
        Func<object, ValueTask<IReadOnlyCollection<object?>>> finalAction)
    {
        if (!behaviors.TryPop(out IBehavior? nextBehavior))
            return finalAction(message);

        return nextBehavior.HandleAsync(
            message,
            nextMessage =>
                ExecuteBehaviorsPipelineAsync(behaviors, nextMessage, finalAction));
    }

    private ValueTask<IReadOnlyCollection<object?>> PublishAsync(
        object message,
        bool throwIfUnhandled,
        ExecutionFlow executionFlow)
    {
        Check.NotNull(message, nameof(message));

        return ExecuteBehaviorsPipelineAsync(
            _behaviorsProvider.CreateStack(),
            message,
            finalMessage => PublishCoreAsync(finalMessage, throwIfUnhandled, executionFlow));
    }

    private async ValueTask<IReadOnlyCollection<object?>> PublishCoreAsync(
        object message,
        bool throwIfUnhandled,
        ExecutionFlow executionFlow)
    {
        IReadOnlyCollection<MethodInvocationResult> resultsCollection =
            await InvokeSubscribedMethodsAsync(message, executionFlow).ConfigureAwait(false);

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
        ExecutionFlow executionFlow) =>
        (await InvokeExclusiveMethodsAsync(message, executionFlow).ConfigureAwait(false))
        .Union(await InvokeNonExclusiveMethodsAsync(message, executionFlow).ConfigureAwait(false))
        .ToList();

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeExclusiveMethodsAsync(
        object message,
        ExecutionFlow executionFlow) =>
        (await _subscribedMethodsCache.GetExclusiveMethods(message)
            .SelectAsync(method => SubscribedMethodInvoker.InvokeAsync(method, message, _serviceProvider, executionFlow))
            .ConfigureAwait(false))
        .ToList();

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeNonExclusiveMethodsAsync(
        object message,
        ExecutionFlow executionFlow) =>
        (await _subscribedMethodsCache.GetNonExclusiveMethods(message)
            .ParallelSelectAsync(method => SubscribedMethodInvoker.InvokeAsync(method, message, _serviceProvider, executionFlow))
            .ConfigureAwait(false))
        .ToList();
}
