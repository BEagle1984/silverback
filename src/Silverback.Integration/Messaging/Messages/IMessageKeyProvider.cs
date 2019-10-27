// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IMessageKeyProvider
    {
        /// <summary>
        /// Returns a boolean value indicating whether this <see cref="IMessageKeyProvider"/>
        /// implementation is able to handle the specified message.
        /// </summary>
        bool CanHandle(object message);

        /// <summary>
        /// Returns the key currently set on the specified message.
        /// </summary>
        string GetKey(object message);

        /// <summary>
        /// Ensures that the key has been initialized (to a unique value) and returns it.
        /// </summary>
        string EnsureKeyIsInitialized(object message);
    }
}
