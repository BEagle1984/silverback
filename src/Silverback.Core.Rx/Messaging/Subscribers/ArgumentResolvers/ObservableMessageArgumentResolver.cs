// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class ObservableMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        public bool CanResolve(Type parameterType) =>
            parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(IObservable<>);

        public Type GetMessageType(Type parameterType) =>
            parameterType.GetGenericArguments()[0];

        public object GetValue(IEnumerable<object> messages, Type targetMessageType) =>
            messages.ToObservable().OfType(targetMessageType);
    }
}