// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     These resolvers are used to handle the messages collection parameter ( <see cref="IEnumerable{T}" />
    ///     , <see cref="IObservable{T}" />, etc.).
    /// </summary>
    public interface IEnumerableMessageArgumentResolver : IMessageArgumentResolver
    {
        /// <summary>
        ///     Returns the messages collection in a shape that is compatible with the subscribed method.
        /// </summary>
        /// <param name="messages">
        ///     The messages being published.
        /// </param>
        /// <param name="targetMessageType">
        ///     The actual message type being declared by the subscribed method (e.g. <c>
        ///         TMessage
        ///     </c> for an <c>
        ///         IEnumerable&lt;TMessage&gt;
        ///     </c>).
        /// </param>
        /// <returns>
        ///     The actual value to be forwarded to the subscribed method.
        /// </returns>
        object GetValue(IReadOnlyCollection<object> messages, Type targetMessageType);
    }
}
