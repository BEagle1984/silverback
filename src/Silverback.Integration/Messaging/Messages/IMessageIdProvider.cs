// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IMessageIdProvider
    {
        /// <summary>
        ///     Returns a boolean value indicating whether this <see cref="IMessageIdProvider" />
        ///     implementation is able to handle the specified message.
        /// </summary>
        bool CanHandle(object message);

        /// <summary>
        ///     Returns the (unique) identifier of the specified message.
        /// </summary>
        string GetId(object message);

        /// <summary>
        ///     Ensures that the id field has been initialized (to a unique value) and returns it.
        /// </summary>
        string EnsureIdentifierIsInitialized(object message);
    }
}