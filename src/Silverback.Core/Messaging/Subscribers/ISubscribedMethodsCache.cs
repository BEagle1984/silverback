// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     Stores the map of the subscribed methods for each message type.
    /// </summary>
    public interface ISubscribedMethodsCache
    {
        /// <summary>
        ///     Gets a value indicating whether there is at least one subscriber that would potentially receive an
        ///     <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </summary>
        bool HasAnyMessageStreamSubscriber { get; }

        /// <summary>
        ///     Checks whether the specified message would be handled by any of the registered subscribers.
        /// </summary>
        /// <param name="message">
        ///     The message that could be published.
        /// </param>
        /// <returns>
        ///     <c>true</c> if at least a subscriber would be invoked for the specified message, otherwise <c>false</c>.
        /// </returns>
        bool IsSubscribed(object message);
    }
}
