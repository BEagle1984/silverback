// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     Can be placed on a subscribed method to filter the messages to be processed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public abstract class MessageFilterAttribute : Attribute, IMessageFilter
    {
        /// <inheritdoc cref="IMessageFilter.MustProcess" />
        public abstract bool MustProcess(object message);
    }
}
