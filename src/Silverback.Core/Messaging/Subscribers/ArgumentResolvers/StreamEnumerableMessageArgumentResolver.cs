// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Resolves the parameters declared as <see cref="IMessageStreamEnumerable{TMessage}" /> where
///     <c>TMessage</c> is compatible with the type of the message being published.
/// </summary>
public class StreamEnumerableMessageArgumentResolver : IStreamEnumerableMessageArgumentResolver
{
    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        if (!parameterType.IsGenericType)
            return false;

        Type genericTypeDefinition = parameterType.GetGenericTypeDefinition();

        return genericTypeDefinition == typeof(IMessageStreamEnumerable<>) ||
               genericTypeDefinition == typeof(IAsyncEnumerable<>) ||
               genericTypeDefinition == typeof(IEnumerable<>);
    }

    /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
    public Type GetMessageType(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        return parameterType.GetGenericArguments()[0];
    }

    /// <inheritdoc cref="IStreamEnumerableMessageArgumentResolver.GetValue" />
    public ILazyArgumentValue GetValue(
        IMessageStreamProvider streamProvider,
        Type targetMessageType,
        IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        Check.NotNull(streamProvider, nameof(streamProvider));

        return (ILazyArgumentValue)streamProvider.CreateLazyStream(targetMessageType, filters);
    }
}
