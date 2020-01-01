// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class ReadOnlyCollectionMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        public bool CanResolve(Type parameterType) =>
            parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>);

        public Type GetMessageType(Type parameterType) =>
            parameterType.GetGenericArguments()[0];

        public object GetValue(IReadOnlyCollection<object> messages, Type targetMessageType) =>
            messages.OfType(targetMessageType).ToList(targetMessageType);
    }
}