// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public abstract class MessageFilterAttribute : Attribute, IMessageFilter
    {
        /// <inheritdoc cref="IMessageFilter" />
        public abstract bool MustProcess(object message);
    }
}