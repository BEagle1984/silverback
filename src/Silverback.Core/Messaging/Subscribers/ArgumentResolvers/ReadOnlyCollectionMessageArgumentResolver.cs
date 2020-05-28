// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Resolves the parameters declared as <see cref="IReadOnlyCollection{T}" /> where <c>T</c> is
    ///     compatible with the type of the message being published.
    /// </summary>
    public class ReadOnlyCollectionMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        /// <inheritdoc />
        public bool CanResolve(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.IsGenericType &&
                   parameterType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>);
        }

        /// <inheritdoc />
        public Type GetMessageType(Type parameterType)
        {
            Check.NotNull(parameterType, nameof(parameterType));

            return parameterType.GetGenericArguments()[0];
        }

        /// <inheritdoc />
        public object GetValue(IReadOnlyCollection<object> messages, Type targetMessageType) =>
            messages.OfType(targetMessageType).ToList(targetMessageType);
    }
}
