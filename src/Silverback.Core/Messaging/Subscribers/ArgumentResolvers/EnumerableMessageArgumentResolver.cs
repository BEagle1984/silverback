// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class EnumerableMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        public bool CanResolve(Type parameterType) =>
            parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        public Type GetMessageType(Type parameterType) =>
            parameterType.GetGenericArguments()[0];

        public object GetValue(IEnumerable<object> messages, Type targetMessageType) =>
            messages.OfType(targetMessageType);
    }
}