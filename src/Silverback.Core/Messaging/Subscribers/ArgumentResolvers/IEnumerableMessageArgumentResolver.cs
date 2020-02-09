﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public interface IEnumerableMessageArgumentResolver : IMessageArgumentResolver
    {
        object GetValue(IEnumerable<object> messages, Type targetMessageType);
    }
}