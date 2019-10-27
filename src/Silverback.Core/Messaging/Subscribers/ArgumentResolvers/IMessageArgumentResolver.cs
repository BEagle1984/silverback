// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public interface IMessageArgumentResolver : IArgumentResolver
    {
        Type GetMessageType(Type parameterType);
    }
}