// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers
{
    /// <inheritdoc cref="IMessageFilter" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public abstract class MessageFilterAttribute : Attribute, IMessageFilter
    {
        public abstract bool MustProcess(object message);
    }
}