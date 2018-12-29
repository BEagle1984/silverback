// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Used to identify the methods that have to be subscribed to the messages stream.
    /// The decorated method must have a single input parameter of type <see cref="IMessage"/>
    /// or derived type. The methods can be async (returning a Task).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SubscribeAttribute : Attribute
    {
    }
}