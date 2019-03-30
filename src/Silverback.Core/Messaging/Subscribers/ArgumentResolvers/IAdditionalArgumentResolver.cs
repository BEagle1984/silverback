// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public interface IAdditionalArgumentResolver : IArgumentResolver
    {
        object GetValue(Type parameterType);
    }
}