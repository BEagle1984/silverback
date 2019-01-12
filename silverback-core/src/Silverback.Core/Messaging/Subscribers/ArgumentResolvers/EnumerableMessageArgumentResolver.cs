// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class EnumerableMessageArgumentResolver : IEnumerableMessageArgumentResolver
    {
        public bool CanResolve(Type parameterType) =>
            typeof(IEnumerable<object>).IsAssignableFrom(parameterType);

        public Type GetMessageType(Type parameterType) =>
            parameterType.GetInterfaces()
                .First(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .GetGenericArguments()[0];

        public object GetValue(IEnumerable<object> messages, Type targetMessageType) =>
            messages.OfType(targetMessageType);
    }
}