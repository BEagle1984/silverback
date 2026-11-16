// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Resolves the parameters declared as <see cref="Messages.IMessageStreamObservable{TMessage}" /> where
///     <c>TMessage</c> is compatible with the type of the message being published.
/// </summary>
public class ObservableStreamMessageArgumentResolver : IStreamEnumerableMessageArgumentResolver
{
    private MethodInfo? _createObservableMethodInfo;

    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        if (!parameterType.IsGenericType)
            return false;

        Type genericTypeDefinition = parameterType.GetGenericTypeDefinition();

        return genericTypeDefinition == typeof(IMessageStreamObservable<>) ||
               genericTypeDefinition == typeof(IObservable<>);
    }

    /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
    public Type GetMessageType(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        return parameterType.GetGenericArguments()[0];
    }

    /// <inheritdoc cref="IStreamEnumerableMessageArgumentResolver.GetValue" />
    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Only the private generic factory method declared in this same class is looked up, to be closed over the runtime message type")]
    public ILazyArgumentValue GetValue(
        IMessageStreamProvider streamProvider,
        Type targetMessageType,
        IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        Check.NotNull(streamProvider, nameof(streamProvider));

        _createObservableMethodInfo ??= typeof(ObservableStreamMessageArgumentResolver).GetMethod(
            "CreateObservable",
            BindingFlags.Static | BindingFlags.NonPublic);

        return (ILazyArgumentValue)_createObservableMethodInfo!
            .MakeGenericMethod(targetMessageType)
            .Invoke(this, [streamProvider.CreateLazyStream(targetMessageType, filters)])!;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via Reflection")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked via Reflection")]
    private static LazyMessageStreamObservable<TMessage> CreateObservable<TMessage>(ILazyMessageStreamEnumerable<TMessage> enumerable) =>
        new(enumerable);
}
