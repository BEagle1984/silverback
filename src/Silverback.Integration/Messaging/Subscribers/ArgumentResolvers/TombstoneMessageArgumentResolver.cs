// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Resolves the parameters declared as <see cref="ITombstone" /> or <see cref="ITombstone{T}" />.
/// </summary>
public class TombstoneMessageArgumentResolver : ISingleMessageArgumentResolver
{
    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        if (!parameterType.IsGenericType)
            return parameterType == typeof(ITombstone) || parameterType == typeof(Tombstone);

        Type genericTypeDefinition = parameterType.GetGenericTypeDefinition();

        return genericTypeDefinition == typeof(ITombstone<>) ||
               genericTypeDefinition == typeof(Tombstone<>);
    }

    /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
    public Type GetMessageType(Type parameterType)
    {
        Check.NotNull(parameterType, nameof(parameterType));

        if (!parameterType.IsGenericType)
            return typeof(IInboundEnvelope);

        return typeof(IInboundEnvelope<>).MakeGenericType(parameterType.GetGenericArguments()[0]);
    }

    /// <inheritdoc cref="ISingleMessageArgumentResolver.GetValue" />
    public object GetValue(object? message, Type parameterType)
    {
        IInboundEnvelope envelope = (IInboundEnvelope)Check.NotNull(message, nameof(message));
        Check.NotNull(parameterType, nameof(parameterType));

        string? messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

        if (parameterType.IsGenericType)
        {
            Type messageType = parameterType.GetGenericArguments()[0];
            return Activator.CreateInstance(typeof(Tombstone<>).MakeGenericType(messageType), messageId)!;
        }

        return new Tombstone(messageId);
    }
}
