// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
        _behaviorsProvider = Check.NotNull(behaviorsProvider);
        _serviceProvider = Check.NotNull(serviceProvider);
        _logger = Check.NotNull(logger);

        _isInServiceScope = serviceProvider != rootServiceProvider.ServiceProvider;

        _subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();
    }

    /// <inheritdoc cref="IPublisher.Context" />
    public ISilverbackContext Context => field ??= _isInServiceScope
        ? _serviceProvider.GetRequiredService<ISilverbackContext>() // use the scoped context only if the publisher is used in a service scope
        : new SilverbackContext(_serviceProvider); // otherwise create a transient one, but some stuff might not work as expected

    /// <inheritdoc cref="IPublisher.Publish(object, bool)" />
    public void Publish(object message, bool throwIfUnhandled = false) =>
        PublishAsync(message, throwIfUnhandled, null, ExecutionFlow.Sync, CancellationToken.None).SafeWait();

    /// <inheritdoc cref="IPublisher.Publish{TResult}(object, bool)" />
    public IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled = false) =>
        [.. CastResults<TResult>(PublishAsync(message, throwIfUnhandled, typeof(TResult), ExecutionFlow.Sync, CancellationToken.None).SafeWait())];

    /// <inheritdoc cref="IPublisher.PublishAsync(object, CancellationToken)" />
    public Task PublishAsync(object message, CancellationToken cancellationToken = default) =>
        PublishAsync(message, false, cancellationToken);

    /// <inheritdoc cref="IPublisher.PublishAsync(object, bool, CancellationToken)" />
    public async Task PublishAsync(object message, bool throwIfUnhandled, CancellationToken cancellationToken = default) =>
        await PublishAsync(message, throwIfUnhandled, null, ExecutionFlow.Async, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, CancellationToken)" />
    public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, CancellationToken cancellationToken = default) =>
        PublishAsync<TResult>(message, false, cancellationToken);

    /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, bool, CancellationToken)" />
    public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(
        object message,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        [.. CastResults<TResult>(await PublishAsync(message, throwIfUnhandled, typeof(TResult), ExecutionFlow.Async, cancellationToken).ConfigureAwait(false))];

    // Republish and RepublishAsync republish a message returned by a subscriber and return the (uncast) results of its subscribers. The
    // expected result type is forwarded to discard the incompatible null results according to the result type declared by the
    // subscribers of the republished message. The non-null results that aren't of the expected type are discarded right away as well,
    // instead of being collected until the original publish completes, only to be discarded by CastResults. As for the public
    // Publish/PublishAsync, an unhandled message doesn't throw and no cancellation token is passed. Like the public Publish, Republish
    // blocks the calling thread until the message is republished.
    internal IReadOnlyCollection<object?> Republish(object message, Type resultType) =>
        DiscardIncompatibleResults(
            PublishAsync(message, false, resultType, ExecutionFlow.Sync, CancellationToken.None).SafeWait(),
            resultType);

    internal async ValueTask<IReadOnlyCollection<object?>> RepublishAsync(object message, Type resultType) =>
        DiscardIncompatibleResults(
            await PublishAsync(message, false, resultType, ExecutionFlow.Async, CancellationToken.None).ConfigureAwait(false),
            resultType);

    // A null result carries no runtime type, so it is matched against the expected result type in PublishCoreAsync, using the result type
    // declared by the subscriber, which isn't available anymore in CastResults. The null is treated like a non-null value returned by the
    // same subscriber: it is kept when a value of the declared type may be of the expected type, i.e. the same type, a derived type or a
    // base type (e.g. object), an interface that a derived type may implement, or an array that a covariant array (e.g. a string[] held
    // by an object[]) may be or implement, or a construction of the same generic delegate that a variant delegate (e.g. a Func<string>
    // held by a Func<object>) may be. The null returned by a subscriber declaring an unrelated type (e.g. an int? subscriber when
    // publishing for a string?) is discarded, as any value it may return would be.
    private static bool IsIncompatibleNullResult(
        MethodInvocationResult invocationResult,
        [NotNullWhen(true)] Type? resultType) =>
        invocationResult is { ReturnValue: null, DeclaredResultType: not null } &&
        resultType != null &&
        !MayBeOfType(invocationResult.DeclaredResultType, resultType);

    private static bool MayBeOfType(Type declaredType, Type expectedType)
    {
        // A boxed Nullable<T> is a boxed T (or null)
        declaredType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

        return expectedType.IsAssignableFrom(declaredType) ||
               declaredType.IsAssignableFrom(expectedType) ||
               MayBeCovariantArrayOfType(declaredType, expectedType) ||
               MayBeCovariantArrayOfType(expectedType, declaredType) ||
               MayBeVariantDelegateOfType(declaredType, expectedType) ||
               declaredType.IsInterface && !expectedType.IsSealed ||
               expectedType.IsInterface && !declaredType.IsSealed;
    }

    // An array type is sealed, but an array of a reference type may hold an array of a derived element type (array covariance), e.g. an
    // object[] may be a string[], which is an IEnumerable<string>. A value of such an array type may therefore also be of the other type
    // if the other type is an array, or a generic collection interface implemented by the arrays (e.g. IEnumerable<T> or
    // IReadOnlyList<T>), whose element type may be of the array element type.
    private static bool MayBeCovariantArrayOfType(Type arrayType, Type otherType)
    {
        if (!arrayType.IsArray || arrayType.GetElementType() is not { IsValueType: false } elementType)
            return false;

        if (otherType.IsArray)
        {
            return otherType.IsSZArray == arrayType.IsSZArray &&
                   otherType.GetArrayRank() == arrayType.GetArrayRank() &&
                   otherType.GetElementType() is { IsValueType: false } otherElementType &&
                   MayBeOfType(elementType, otherElementType);
        }

        if (!otherType.IsInterface || !otherType.IsGenericType || otherType.GenericTypeArguments[0].IsValueType)
            return false;

        Type otherTypeDefinition = otherType.GetGenericTypeDefinition();

        return arrayType.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType &&
                                                              interfaceType.GetGenericTypeDefinition() == otherTypeDefinition) &&
               MayBeOfType(elementType, otherType.GenericTypeArguments[0]);
    }

    // A delegate type is sealed, but a generic delegate with variant type parameters may hold a delegate with different type arguments
    // (delegate variance), e.g. a Func<IComparable> may be a Func<string>, which is a Func<IConvertible>. A value of such a delegate type
    // may therefore also be of another construction of the same generic delegate if each pair of type arguments is either identical or,
    // for a variant type parameter, made of reference types that a common type argument may be converted from (covariant) or to
    // (contravariant, always possible via object).
    private static bool MayBeVariantDelegateOfType(Type declaredType, Type expectedType)
    {
        if (!declaredType.IsGenericType ||
            !expectedType.IsGenericType ||
            !typeof(Delegate).IsAssignableFrom(declaredType) ||
            declaredType.GetGenericTypeDefinition() != expectedType.GetGenericTypeDefinition())
        {
            return false;
        }

        Type[] typeParameters = declaredType.GetGenericTypeDefinition().GetGenericArguments();
        Type[] declaredTypeArguments = declaredType.GenericTypeArguments;
        Type[] expectedTypeArguments = expectedType.GenericTypeArguments;

        return typeParameters.Select((typeParameter, index) => MayBeVariantTypeArgument(
                typeParameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask,
                declaredTypeArguments[index],
                expectedTypeArguments[index]))
            .All(mayBeVariant => mayBeVariant);
    }

    // Variance applies to reference type arguments only
    private static bool MayBeVariantTypeArgument(GenericParameterAttributes variance, Type declaredTypeArgument, Type expectedTypeArgument) =>
        declaredTypeArgument == expectedTypeArgument ||
        !declaredTypeArgument.IsValueType &&
        !expectedTypeArgument.IsValueType &&
        variance switch
        {
            GenericParameterAttributes.Covariant => MayBeOfType(declaredTypeArgument, expectedTypeArgument),
            GenericParameterAttributes.Contravariant => true,
            _ => false
        };

    private ValueTask<IReadOnlyCollection<object?>> ExecuteBehaviorsPipelineAsync(
        Stack<IBehavior> behaviors,
        object message,
        bool throwIfUnhandled,
        Type? resultType,
        Func<object, bool, Type?, ExecutionFlow, CancellationToken, ValueTask<IReadOnlyCollection<object?>>> finalAction,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        if (!behaviors.TryPop(out IBehavior? nextBehavior))
            return finalAction(message, throwIfUnhandled, resultType, executionFlow, cancellationToken);

        return nextBehavior.HandleAsync(this, message, NextAsync, cancellationToken);

        ValueTask<IReadOnlyCollection<object?>> NextAsync(object nextMessage) =>
            ExecuteBehaviorsPipelineAsync(behaviors, nextMessage, throwIfUnhandled, resultType, finalAction, executionFlow, cancellationToken);
    }

    private ValueTask<IReadOnlyCollection<object?>> PublishAsync(
        object message,
        bool throwIfUnhandled,
        Type? resultType,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        Check.NotNull(message);

        return ExecuteBehaviorsPipelineAsync(
            _behaviorsProvider.CreateStack(),
            message,
            throwIfUnhandled,
            resultType,
            PublishCoreAsync,
            executionFlow,
            cancellationToken);
    }

    private async ValueTask<IReadOnlyCollection<object?>> PublishCoreAsync(
        object message,
        bool throwIfUnhandled,
        Type? resultType,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MethodInvocationResult> resultsCollection =
            await InvokeSubscribedMethodsAsync(message, resultType, executionFlow, cancellationToken).ConfigureAwait(false);

        bool handled = resultsCollection.Any(invocationResult => invocationResult.WasInvoked);

        if (!handled && throwIfUnhandled)
            throw new UnhandledMessageException(message);

        List<object?> results = [];

        foreach (MethodInvocationResult invocationResult in resultsCollection)
        {
            // The results of the republished messages were already matched against the expected result type by Republish or
            // RepublishAsync, using the result types declared by their own subscribers for the null results
            if (!invocationResult.HasReturnValue)
            {
                results.AddRange(invocationResult.ForwardedResults);
                continue;
            }

            if (IsIncompatibleNullResult(invocationResult, resultType))
            {
                _logger.LogSubscriberResultDiscarded(invocationResult.DeclaredResultType?.FullName, resultType.FullName!);
                continue;
            }

            results.Add(invocationResult.ReturnValue);
        }

        return results;
    }

    // The null results were already matched against the expected result type in PublishCoreAsync. The non-null results are matched
    // like in CastResults: a boxed Nullable<T> is a boxed T.
    private IReadOnlyCollection<object?> DiscardIncompatibleResults(IReadOnlyCollection<object?> results, Type resultType)
    {
        Type expectedType = Nullable.GetUnderlyingType(resultType) ?? resultType;

        if (results.All(result => result == null || expectedType.IsInstanceOfType(result)))
            return results;

        List<object?> compatibleResults = [];

        foreach (object? result in results)
        {
            if (result == null || expectedType.IsInstanceOfType(result))
                compatibleResults.Add(result);
            else
                _logger.LogSubscriberResultDiscarded(result.GetType().FullName, resultType.FullName!);
        }

        return compatibleResults;
    }

    private IEnumerable<TResult> CastResults<TResult>(IReadOnlyCollection<object?> results)
    {
        foreach (object? result in results)
        {
            if (result is TResult castResult)
                yield return castResult;
            else if (result == null && default(TResult) is null) // TResult is a reference type or a Nullable<T>
                yield return default!;
            else
                _logger.LogSubscriberResultDiscarded(result?.GetType().FullName, typeof(TResult).FullName!);
        }
    }

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeSubscribedMethodsAsync(
        object message,
        Type? resultType,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken) =>
        [.. (await InvokeExclusiveMethodsAsync(message, resultType, executionFlow, cancellationToken).ConfigureAwait(false)).Union(await InvokeNonExclusiveMethodsAsync(message, resultType, executionFlow, cancellationToken).ConfigureAwait(false))];

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeExclusiveMethodsAsync(
        object message,
        Type? resultType,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SubscribedMethod> methods = _subscribedMethodsCache.GetExclusiveMethods(message, _serviceProvider);

        if (methods.Count == 0)
            return [];

        if (methods.Count == 1)
            return [await InvokeAsync(methods[0]).ConfigureAwait(false)];

        return
        [
            .. await methods
                .SelectAsync(InvokeAsync)
                .ConfigureAwait(false)
        ];

        ValueTask<MethodInvocationResult> InvokeAsync(SubscribedMethod method) =>
            SubscribedMethodInvoker.InvokeAsync(this, method, message, resultType, _serviceProvider, executionFlow, cancellationToken);
    }

    private async ValueTask<IReadOnlyCollection<MethodInvocationResult>> InvokeNonExclusiveMethodsAsync(
        object message,
        Type? resultType,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SubscribedMethod> methods = _subscribedMethodsCache.GetNonExclusiveMethods(message, _serviceProvider);

        if (methods.Count == 0)
            return [];

        if (methods.Count == 1)
            return [await InvokeAsync(methods[0]).ConfigureAwait(false)];

        return
        [
            .. await methods
                .ParallelSelectAsync(InvokeAsync)
                .ConfigureAwait(false)
        ];

        ValueTask<MethodInvocationResult> InvokeAsync(SubscribedMethod method) =>
            SubscribedMethodInvoker.InvokeAsync(this, method, message, resultType, _serviceProvider, executionFlow, cancellationToken);
    }
}
