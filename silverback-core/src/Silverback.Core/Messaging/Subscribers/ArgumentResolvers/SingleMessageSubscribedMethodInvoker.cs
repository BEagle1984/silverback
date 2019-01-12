// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class SingleMessageArgumentResolver : ISingleMessageArgumentResolver
    {
        public bool CanResolve(Type parameterType) => true;

        public Type GetMessageType(Type parameterType) => parameterType;

        public object GetValue(object message) => message;
    }
}