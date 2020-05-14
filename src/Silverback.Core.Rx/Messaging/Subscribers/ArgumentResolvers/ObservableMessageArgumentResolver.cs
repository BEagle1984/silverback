// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Resolves the parameters declared as <see cref="IObservable{T}" /> where <c> T </c> is
    ///     compatible with the type of the message being published.
    /// </summary>
    public class ObservableMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        /// <inheritdoc />
        [SuppressMessage("ReSharper", "CA1062", Justification = Justifications.CalledBySilverback)]
        public bool CanResolve(Type parameterType) =>
            parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(IObservable<>);

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "CA1062", Justification = Justifications.CalledBySilverback)]
        public Type GetMessageType(Type parameterType) =>
            parameterType.GetGenericArguments()[0];

        /// <inheritdoc />
        public object GetValue(IReadOnlyCollection<object> messages, Type targetMessageType) =>
            messages.ToObservable().OfType(targetMessageType);
    }
}
