// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public interface ISingleMessageArgumentResolver : IMessageArgumentResolver
    {
        object GetValue(object message);
    }
}