// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    [Obsolete("This feature will be removed in a future release. Please use a behavior instead.")]
    public interface IMessageIdProvider
    {
        /// <summary>
        ///     Returns a boolean value indicating whether this <see cref="IMessageIdProvider" />
        ///     implementation is able to handle the specified message.
        /// </summary>
        bool CanHandle(object message);

        /// <summary>
        ///     Ensures that the id field has been initialized (to a unique value) and returns it.
        /// </summary>
        string EnsureIdentifierIsInitialized(object message);
    }
}