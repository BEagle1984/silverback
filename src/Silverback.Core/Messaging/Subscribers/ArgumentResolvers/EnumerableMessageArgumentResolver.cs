// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Resolves the parameters declared as <see cref="IEnumerable{T}" /> where <c>T</c> is compatible with
    ///     the type of the message being published.
    /// </summary>
    public class EnumerableMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        /// <inheritdoc cref="IArgumentResolver.CanResolve" />
        public bool CanResolve(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.IsGenericType &&
                   parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
        public Type GetMessageType(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.GetGenericArguments()[0];
        }

        /// <inheritdoc cref="IEnumerableMessageArgumentResolver.GetValue" />
        public object GetValue(IReadOnlyCollection<object> messages, Type targetMessageType)
        {
            Check.NotNull(messages, nameof(messages));

            return messages.OfType(targetMessageType).ToList(targetMessageType);
        }
    }
}
