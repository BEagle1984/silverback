// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Resolves the parameters declared as <see cref="IMessageStreamEnumerable{TMessage}" /> where <c>T</c>
    ///     is compatible with
    ///     the type of the message being published.
    /// </summary>
    public class StreamEnumerableMessageArgumentResolver : IStreamEnumerableMessageArgumentResolver
    {
        /// <inheritdoc cref="IArgumentResolver.CanResolve" />
        public bool CanResolve(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.IsGenericType &&
                   parameterType.GetGenericTypeDefinition() == typeof(IMessageStreamEnumerable<>);
        }

        /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
        public Type GetMessageType(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.GetGenericArguments()[0];
        }

        /// <inheritdoc cref="IStreamEnumerableMessageArgumentResolver.GetValue" />
        public object GetValue(object message, Type targetMessageType)
        {
            if (message is IMessageStreamEnumerable<object>)
                return ((IWritableMessageStream)message).CreateLinkedStream(targetMessageType);

            throw new NotImplementedException("TODO: Push message to stream?");
        }
    }
}
